using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Data;
using AutodeskSoftwareManager.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AutodeskSoftwareManager.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    public ObservableCollection<DeploymentRecord> History { get; } = [];
    public ICollectionView HistoryView { get; }

    // ── Filter ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string   _searchText      = string.Empty;
    [ObservableProperty] private string   _selectedPackage = "All Packages";
    [ObservableProperty] private string   _selectedResult  = "All";
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;

    public ObservableCollection<string> PackageNames { get; } = ["All Packages"];

    public HistoryViewModel()
    {
        HistoryView = CollectionViewSource.GetDefaultView(History);
        HistoryView.Filter = ApplyFilter;
        LoadFromDb();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Refresh() => LoadFromDb();

    [RelayCommand]
    private void ExportCsv()
    {
        var dlg = new SaveFileDialog
        {
            Title    = "Export History",
            Filter   = "CSV files (*.csv)|*.csv",
            FileName = $"deploy_history_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("Started,Computer,OU,Package,Target Version,Result,Duration,Message");
        foreach (DeploymentRecord r in HistoryView)
        {
            sb.AppendLine($"{Q(r.StartedAtDisplay)},{Q(r.ComputerName)},{Q(r.ComputerOU)}," +
                          $"{Q(r.PackageName)},{Q(r.TargetVersion)},{Q(r.ResultText)}," +
                          $"{Q(r.DurationDisplay)},{Q(r.Message)}");
        }
        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    partial void OnSearchTextChanged(string value)      => HistoryView.Refresh();
    partial void OnSelectedPackageChanged(string value) => HistoryView.Refresh();
    partial void OnSelectedResultChanged(string value)  => HistoryView.Refresh();
    partial void OnDateFromChanged(DateTime? value)     => HistoryView.Refresh();
    partial void OnDateToChanged(DateTime? value)       => HistoryView.Refresh();

    private bool ApplyFilter(object obj)
    {
        if (obj is not DeploymentRecord r) return false;
        if (!string.IsNullOrEmpty(SearchText) &&
            !r.ComputerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) return false;
        if (SelectedPackage != "All Packages" && r.PackageName != SelectedPackage) return false;
        if (SelectedResult == "Success" && !r.Success) return false;
        if (SelectedResult == "Failed"  &&  r.Success) return false;
        if (DateFrom.HasValue && DateTime.TryParse(r.StartedAt, out var s) && s < DateFrom.Value) return false;
        if (DateTo.HasValue   && DateTime.TryParse(r.StartedAt, out var t) && t > DateTo.Value.AddDays(1)) return false;
        return true;
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    public void LoadFromDb()
    {
        History.Clear();
        PackageNames.Clear();
        PackageNames.Add("All Packages");

        foreach (var r in App.Db.GetHistory())
        {
            History.Add(r);
            if (!PackageNames.Contains(r.PackageName))
                PackageNames.Add(r.PackageName);
        }
    }

    private static string Q(string s) => s.Contains(',') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
}
