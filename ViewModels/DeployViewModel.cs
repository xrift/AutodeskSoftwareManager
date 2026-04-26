using System.Collections.ObjectModel;
using System.Windows;
using AutodeskSoftwareManager.Models;
using AutodeskSoftwareManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutodeskSoftwareManager.ViewModels;

// Row shown in the deploy target grid
public partial class DeployTargetRow : ObservableObject
{
    public int    ComputerId      { get; set; }
    public string ComputerName    { get; set; } = string.Empty;
    public string OU              { get; set; } = string.Empty;
    public string CurrentVersion  { get; set; } = string.Empty;
    public string TargetVersion   { get; set; } = string.Empty;
    public bool   IsOutdated      { get; set; }
    public string LoggedInUser    { get; set; } = string.Empty;
    public string LastDeploy      { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(StatusColor))]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(StatusColor))]
    private DeployStatus _deployStatus = DeployStatus.Pending;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public string StatusText => DeployStatus switch
    {
        DeployStatus.Success   => "Success",
        DeployStatus.Failed    => "Failed",
        DeployStatus.Cancelled => "Cancelled",
        DeployStatus.Queued    => "Queued",
        DeployStatus.Copying   => "Copying",
        DeployStatus.Launching => "Launching",
        DeployStatus.Running   => "Running",
        _                      => IsOutdated ? "Outdated" : "Up to Date"
    };

    public string StatusColor => DeployStatus switch
    {
        DeployStatus.Success   => "#27AE60",
        DeployStatus.Failed    => "#E74C3C",
        DeployStatus.Cancelled => "#95A5A6",
        DeployStatus.Queued or DeployStatus.Copying
            or DeployStatus.Launching or DeployStatus.Running => "#3498DB",
        _                      => IsOutdated ? "#E67E22" : "#27AE60"
    };
}

public partial class DeployViewModel : ObservableObject
{
    public ObservableCollection<DeploymentPackage> Packages { get; } = [];
    public ObservableCollection<DeployTargetRow>   Targets  { get; } = [];

    [ObservableProperty] private DeploymentPackage? _selectedPackage;
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _progressText  = string.Empty;
    [ObservableProperty] private int    _progressValue;
    [ObservableProperty] private string _targetFilter  = "Outdated only";

    public IEnumerable<DeployTargetRow> SelectedTargets => Targets.Where(t => t.IsSelected);

    private CancellationTokenSource? _cts;

    public DeployViewModel() => RefreshPackages();

    // ── Commands ──────────────────────────────────────────────────────────────

    partial void OnSelectedPackageChanged(DeploymentPackage? _) => RefreshTargets();
    partial void OnTargetFilterChanged(string _) => RefreshTargets();

    [RelayCommand]
    private void SelectAllOutdated()
    {
        foreach (var t in Targets) t.IsSelected = t.IsOutdated;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var t in Targets) t.IsSelected = false;
    }

    [RelayCommand(CanExecute = nameof(CanDeploy))]
    private async Task Deploy() => await DeployAsync();

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    // ── Data loading ──────────────────────────────────────────────────────────

    public void RefreshPackages()
    {
        var prev = SelectedPackage?.Id;
        Packages.Clear();
        foreach (var p in App.Db.GetPackages(activeOnly: true))
            Packages.Add(p);

        SelectedPackage = Packages.FirstOrDefault(p => p.Id == prev) ?? Packages.FirstOrDefault();
    }

    private void RefreshTargets()
    {
        Targets.Clear();
        if (SelectedPackage is null) return;

        // Get latest history per computer for this package
        var history = App.Db.GetHistory()
            .Where(h => h.PackageId == SelectedPackage.Id)
            .GroupBy(h => h.ComputerId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.StartedAt).First());

        // Get current installed version per computer for this product family
        var installed = App.Db.GetAllInstalled()
            .Where(p => p.ProductFamily == SelectedPackage.ProductFamily)
            .GroupBy(p => p.ComputerId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.DisplayVersion).First());

        foreach (var computer in App.Db.GetAllComputers().Where(c => c.IsOnline))
        {
            installed.TryGetValue(computer.Id, out var inst);
            history.TryGetValue(computer.Id, out var hist);

            var curVer     = inst?.DisplayVersion ?? string.Empty;
            var targetVer  = SelectedPackage.TargetVersion;
            bool outdated  = IsOutdated(curVer, targetVer);

            if (TargetFilter == "Outdated only"  && !outdated) continue;
            if (TargetFilter == "Not installed"  && !string.IsNullOrEmpty(curVer)) continue;

            Targets.Add(new DeployTargetRow
            {
                ComputerId     = computer.Id,
                ComputerName   = computer.Name,
                OU             = computer.OU,
                CurrentVersion = string.IsNullOrEmpty(curVer) ? "—" : curVer,
                TargetVersion  = targetVer,
                IsOutdated     = outdated,
                LoggedInUser   = computer.LoggedInUser,
                LastDeploy     = hist is not null
                                 ? (DateTime.TryParse(hist.StartedAt, out var dt)
                                    ? dt.ToString("yyyy-MM-dd") : hist.StartedAt)
                                 : "Never"
            });
        }
    }

    private async Task DeployAsync()
    {
        if (SelectedPackage is null) return;
        var targets = SelectedTargets.ToList();
        if (targets.Count == 0) return;

        _cts = new CancellationTokenSource();
        IsBusy = true;
        DeployCommand.NotifyCanExecuteChanged();

        int total = targets.Count, done = 0, succeeded = 0;
        ProgressText  = $"Deploying to {total} computer(s)...";
        ProgressValue = 0;

        foreach (var t in targets) { t.DeployStatus = DeployStatus.Queued; t.StatusMessage = "Queued"; }

        using var sem = new SemaphoreSlim(App.Settings.MaxDeployThreads);
        var pkg       = SelectedPackage;

        var tasks = targets.Select(async target =>
        {
            await sem.WaitAsync(_cts.Token);
            var record = new DeploymentRecord
            {
                PackageId    = pkg.Id,   ComputerId    = target.ComputerId,
                PackageName  = pkg.Name, ComputerName  = target.ComputerName,
                ComputerOU   = target.OU,TargetVersion = pkg.TargetVersion,
                StartedAt    = DateTime.Now.ToString("o")
            };
            int recordId = App.Db.InsertDeploymentRecord(record);

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    target.DeployStatus  = DeployStatus.Copying;
                    target.StatusMessage = "Copying installer...";
                });

                var progress = new Progress<string>(msg =>
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        target.StatusMessage = msg;
                        target.DeployStatus  = msg.StartsWith("Launch") ? DeployStatus.Launching
                                             : msg.StartsWith("Wait")   ? DeployStatus.Running
                                             : DeployStatus.Copying;
                    }));

                var result = await Deployer.DeployAsync(
                    target.ComputerName, pkg.InstallerPath, pkg.SilentArgs,
                    App.Settings.DeployTimeoutSec, progress, _cts.Token);

                App.Db.FinalizeDeploymentRecord(recordId, result.Success, result.Message);

                int completed = Interlocked.Increment(ref done);
                if (result.Success) Interlocked.Increment(ref succeeded);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    target.DeployStatus  = result.Success ? DeployStatus.Success : DeployStatus.Failed;
                    target.StatusMessage = result.Message;
                    ProgressValue = (int)((double)completed / total * 100);
                    ProgressText  = $"Deploying — {completed}/{total} ({succeeded} succeeded)";
                });
            }
            catch (OperationCanceledException)
            {
                App.Db.FinalizeDeploymentRecord(recordId, false, "Cancelled");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    target.DeployStatus  = DeployStatus.Cancelled;
                    target.StatusMessage = "Cancelled";
                });
            }
            finally { sem.Release(); }
        }).ToList();

        try   { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) { }

        ProgressText  = $"Complete — {succeeded} succeeded, {done - succeeded} failed of {total}";
        ProgressValue = 100;
        IsBusy        = false;
        DeployCommand.NotifyCanExecuteChanged();
        App.HistoryVm.LoadFromDb();
    }

    private bool CanDeploy() => !IsBusy && SelectedPackage is not null && SelectedTargets.Any();

    private static bool IsOutdated(string installed, string target)
    {
        if (string.IsNullOrEmpty(installed)) return true; // not installed = needs deploying
        if (string.IsNullOrEmpty(target))    return false;
        if (!Version.TryParse(installed, out var v1)) return false;
        if (!Version.TryParse(target,    out var v2)) return false;
        return v1 < v2;
    }
}
