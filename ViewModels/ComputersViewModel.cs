using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Data;
using AutodeskSoftwareManager.Models;
using AutodeskSoftwareManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutodeskSoftwareManager.ViewModels;

public partial class ComputersViewModel : ObservableObject
{
    public ObservableCollection<Computer> Computers { get; } = [];
    public ICollectionView ComputerView { get; }

    // ── Stats ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _onlineCount;
    [ObservableProperty] private int _offlineCount;
    [ObservableProperty] private int _scannedCount;
    [ObservableProperty] private int _outdatedCount;

    // ── Filter bar ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _searchText    = string.Empty;
    [ObservableProperty] private string _selectedOU    = "All OUs";
    [ObservableProperty] private string _selectedStatus= "All";
    [ObservableProperty] private bool   _hasAutodesk;
    [ObservableProperty] private bool   _hasOutdated;

    public ObservableCollection<string> OUList { get; } = ["All OUs"];

    // ── Progress ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _progressText  = string.Empty;
    [ObservableProperty] private int    _progressValue;

    private CancellationTokenSource? _cts;

    public IEnumerable<Computer> SelectedComputers =>
        Computers.Where(c => c.IsSelected);

    public ComputersViewModel()
    {
        ComputerView = CollectionViewSource.GetDefaultView(Computers);
        ComputerView.Filter = ApplyFilter;
        LoadFromDb();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task ScanNetwork() => await ScanNetworkAsync();

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task ScanInventory() => await ScanInventoryAsync(SelectedComputers.ToList());

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void RefreshStatus() => LoadFromDb();

    [RelayCommand]
    private void Cancel() { _cts?.Cancel(); }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void AddComputer(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var c = new Computer { Name = name.Trim().ToUpperInvariant() };
        App.Db.UpsertComputer(c);
        Computers.Add(c);
        RefreshStats();
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void RemoveSelected()
    {
        foreach (var c in Computers.Where(x => x.IsSelected).ToList())
        {
            App.Db.DeleteComputer(c.Id);
            Computers.Remove(c);
        }
        RefreshStats();
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    partial void OnSearchTextChanged(string value)     => ComputerView.Refresh();
    partial void OnSelectedOUChanged(string value)     => ComputerView.Refresh();
    partial void OnSelectedStatusChanged(string value) => ComputerView.Refresh();
    partial void OnHasAutodeskChanged(bool value)      => ComputerView.Refresh();
    partial void OnHasOutdatedChanged(bool value)      => ComputerView.Refresh();

    private bool ApplyFilter(object obj)
    {
        if (obj is not Computer c) return false;
        if (!string.IsNullOrEmpty(SearchText) &&
            !c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) return false;
        if (SelectedOU != "All OUs" && c.OU != SelectedOU) return false;
        if (SelectedStatus == "Online"  && !c.IsOnline)  return false;
        if (SelectedStatus == "Offline" &&  c.IsOnline)  return false;
        if (HasAutodesk && c.ProductCount == 0)          return false;
        if (HasOutdated && c.OutdatedCount == 0)         return false;
        return true;
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    public void LoadFromDb()
    {
        Computers.Clear();
        OUList.Clear();
        OUList.Add("All OUs");

        foreach (var c in App.Db.GetAllComputers())
        {
            Computers.Add(c);
            if (!string.IsNullOrEmpty(c.OU) && !OUList.Contains(c.OU))
                OUList.Add(c.OU);
        }
        RefreshStats();
        App.MainVm.RefreshCounts();
    }

    private void RefreshStats()
    {
        var (total, online, offline, scanned, outdated) = App.Db.GetComputerStats();
        TotalCount    = total;
        OnlineCount   = online;
        OfflineCount  = offline;
        ScannedCount  = scanned;
        OutdatedCount = outdated;
    }

    // ── Network scan ─────────────────────────────────────────────────────────

    private async Task ScanNetworkAsync()
    {
        _cts = new CancellationTokenSource();
        SetBusy(true);

        try
        {
            ProgressText = "Querying Active Directory...";
            List<AdComputer> adList;
            try
            {
                adList = await AdDiscovery.GetComputersAsync(App.Settings.AdFilter);
            }
            catch (Exception ex)
            {
                ProgressText = $"AD query failed — {ex.Message}";
                return;
            }

            // Upsert newly discovered computers
            foreach (var ad in adList)
            {
                var existing = Computers.FirstOrDefault(
                    c => c.Name.Equals(ad.Name, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    var c = new Computer { Name = ad.Name, Description = ad.Description, OU = ad.OU };
                    App.Db.UpsertComputer(c);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Computers.Add(c);
                        if (!string.IsNullOrEmpty(c.OU) && !OUList.Contains(c.OU))
                            OUList.Add(c.OU);
                    });
                }
                else
                {
                    existing.Description = ad.Description;
                    existing.OU          = ad.OU;
                    App.Db.UpsertComputer(existing);
                }
            }

            // Ping all
            int total = Computers.Count, done = 0, online = 0;
            ProgressText = $"Pinging {total} computers...";

            using var sem = new SemaphoreSlim(App.Settings.MaxScanThreads);

            var tasks = Computers.ToList().Select(async computer =>
            {
                await sem.WaitAsync(_cts.Token);
                try
                {
                    Application.Current.Dispatcher.Invoke(() => computer.IsScanning = true);

                    bool alive = await PingAsync(computer.Name);
                    if (alive) Interlocked.Increment(ref online);

                    var now = DateTime.Now.ToString("o");
                    App.Db.UpdateOnlineStatus(computer.Name, alive, string.Empty,
                        alive ? DateTime.Now : (DateTime.TryParse(computer.LastSeen, out var ls) ? ls : DateTime.MinValue));

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        computer.IsOnline  = alive;
                        computer.IsScanning= false;
                        if (alive) computer.LastSeen = now;
                        int completed = Interlocked.Increment(ref done);
                        ProgressValue = (int)((double)completed / total * 100);
                        ProgressText  = $"Pinged {completed}/{total} — {online} online";
                    });
                }
                finally { sem.Release(); }
            }).ToList();

            await Task.WhenAll(tasks);
            ProgressText  = $"Scan complete — {online} online / {total} total";
            ProgressValue = 100;
        }
        catch (OperationCanceledException) { ProgressText = "Scan cancelled."; }
        finally
        {
            foreach (var c in Computers) c.IsScanning = false;
            SetBusy(false);
            RefreshStats();
            App.MainVm.RefreshCounts();
        }
    }

    // ── Inventory scan ────────────────────────────────────────────────────────

    public async Task ScanInventoryAsync(IReadOnlyList<Computer> targets)
    {
        if (targets.Count == 0) return;
        _cts = new CancellationTokenSource();
        SetBusy(true);

        var catalog   = App.Catalog;
        int total = targets.Count, done = 0;
        ProgressText = $"Scanning inventory on {total} computer(s)...";

        using var sem = new SemaphoreSlim(App.Settings.MaxScanThreads);

        var tasks = targets.Select(async computer =>
        {
            await sem.WaitAsync(_cts.Token);
            try
            {
                Application.Current.Dispatcher.Invoke(() => computer.IsScanning = true);

                var result = await InventoryScanner.ScanAsync(
                    computer.Id, computer.Name, catalog, App.Settings.PingTimeoutMs);

                if (result.IsOnline)
                {
                    App.Db.ReplaceInstalledSoftware(computer.Id, result.Products);
                    App.Db.UpdateLastScan(computer.Id);
                    App.Db.UpdateOnlineStatus(computer.Name, true, result.LoggedInUser, DateTime.Now);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    computer.IsScanning    = false;
                    computer.IsOnline      = result.IsOnline;
                    computer.LoggedInUser  = result.LoggedInUser;
                    computer.LastScan      = DateTime.Now.ToString("o");
                    computer.ProductCount  = result.Products.Count;
                    computer.OutdatedCount = result.Products.Count(p => p.IsOutdated);

                    int completed = Interlocked.Increment(ref done);
                    ProgressValue = (int)((double)completed / total * 100);
                    ProgressText  = $"Scanned {completed}/{total}";
                });
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                Application.Current.Dispatcher.Invoke(() => computer.IsScanning = false);
                Interlocked.Increment(ref done);
            }
            finally { sem.Release(); }
        }).ToList();

        try   { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) { ProgressText = "Scan cancelled."; }
        finally
        {
            foreach (var c in targets) c.IsScanning = false;
            SetBusy(false);
            RefreshStats();
            App.InventoryVm.LoadFromDb();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsIdle => !IsBusy;

    private void SetBusy(bool busy)
    {
        IsBusy = busy;
        ScanNetworkCommand.NotifyCanExecuteChanged();
        ScanInventoryCommand.NotifyCanExecuteChanged();
        RefreshStatusCommand.NotifyCanExecuteChanged();
        AddComputerCommand.NotifyCanExecuteChanged();
        RemoveSelectedCommand.NotifyCanExecuteChanged();
    }

    private static async Task<bool> PingAsync(string host)
    {
        try
        {
            using var ping  = new Ping();
            var reply = await ping.SendPingAsync(host, 1500);
            return reply.Status == IPStatus.Success;
        }
        catch { return false; }
    }
}
