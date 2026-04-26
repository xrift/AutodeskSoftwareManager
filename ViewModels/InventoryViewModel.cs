using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using AutodeskSoftwareManager.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AutodeskSoftwareManager.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    public ObservableCollection<InstalledProduct> Products { get; } = [];
    public ICollectionView ProductView { get; }

    // ── Stats ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _productFamilyCount;
    [ObservableProperty] private int _outdatedCount;
    [ObservableProperty] private int _upToDateCount;

    // ── Filter ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _searchText       = string.Empty;
    [ObservableProperty] private string _selectedFamily   = "All Products";
    [ObservableProperty] private string _selectedStatus   = "All";
    [ObservableProperty] private string _selectedOU       = "All OUs";

    public ObservableCollection<string> FamilyList { get; } = ["All Products"];
    public ObservableCollection<string> OUList     { get; } = ["All OUs"];

    // ── Progress ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _progressText  = string.Empty;
    [ObservableProperty] private int    _progressValue;

    public InventoryViewModel()
    {
        ProductView = CollectionViewSource.GetDefaultView(Products);
        ProductView.Filter = ApplyFilter;
        LoadFromDb();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task ScanSelected() =>
        await App.ComputersVm.ScanInventoryAsync(App.ComputersVm.SelectedComputers.ToList());

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task ScanAllOnline() =>
        await App.ComputersVm.ScanInventoryAsync(
            App.ComputersVm.Computers.Where(c => c.IsOnline).ToList());

    [RelayCommand]
    private void Refresh() => LoadFromDb();

    [RelayCommand]
    private void ExportCsv()
    {
        var dlg = new SaveFileDialog
        {
            Title      = "Export Inventory",
            Filter     = "CSV files (*.csv)|*.csv",
            FileName   = $"inventory_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("Computer,OU,Product,Family,Installed Version,Latest Version,Status,Install Date,Scanned");
        foreach (InstalledProduct p in ProductView)
        {
            sb.AppendLine($"{Q(p.ComputerName)},{Q(p.ComputerOU)},{Q(p.DisplayName)}," +
                          $"{Q(p.ProductFamily)},{Q(p.DisplayVersion)},{Q(p.LatestVersion)}," +
                          $"{Q(p.StatusText)},{Q(p.InstallDate)},{Q(p.ScannedAtDisplay)}");
        }
        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show($"Exported {Products.Count} records.", "Export Complete",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    partial void OnSearchTextChanged(string value)     => ProductView.Refresh();
    partial void OnSelectedFamilyChanged(string value) => ProductView.Refresh();
    partial void OnSelectedStatusChanged(string value) => ProductView.Refresh();
    partial void OnSelectedOUChanged(string value)     => ProductView.Refresh();

    private bool ApplyFilter(object obj)
    {
        if (obj is not InstalledProduct p) return false;
        if (!string.IsNullOrEmpty(SearchText) &&
            !p.ComputerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
            !p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) return false;
        if (SelectedFamily != "All Products" && p.ProductFamily != SelectedFamily) return false;
        if (SelectedOU     != "All OUs"      && p.ComputerOU    != SelectedOU)     return false;
        if (SelectedStatus == "Outdated"  && !p.IsOutdated)   return false;
        if (SelectedStatus == "Up to Date"&& p.IsOutdated)    return false;
        return true;
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    public void LoadFromDb()
    {
        Products.Clear();
        FamilyList.Clear(); FamilyList.Add("All Products");
        OUList.Clear();     OUList.Add("All OUs");

        foreach (var p in App.Db.GetAllInstalled())
        {
            Products.Add(p);
            if (!string.IsNullOrEmpty(p.ProductFamily) && !FamilyList.Contains(p.ProductFamily))
                FamilyList.Add(p.ProductFamily);
            if (!string.IsNullOrEmpty(p.ComputerOU) && !OUList.Contains(p.ComputerOU))
                OUList.Add(p.ComputerOU);
        }

        TotalCount        = Products.Count;
        OutdatedCount     = Products.Count(p => p.IsOutdated);
        UpToDateCount     = Products.Count(p => !p.IsOutdated);
        ProductFamilyCount= FamilyList.Count - 1;
    }

    private bool IsIdle => !IsBusy;

    private static string Q(string s) => s.Contains(',') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
}
