using System.Diagnostics;
using System.IO;
using System.Windows;
using AutodeskSoftwareManager.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutodeskSoftwareManager.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private int    _maxScanThreads;
    [ObservableProperty] private int    _maxDeployThreads;
    [ObservableProperty] private int    _pingTimeoutMs;
    [ObservableProperty] private int    _deployTimeoutSec;
    [ObservableProperty] private string _defaultSilentArgs = string.Empty;
    [ObservableProperty] private string _adFilter          = string.Empty;
    [ObservableProperty] private string _dbPath            = string.Empty;

    public string AppVersion { get; } =
        System.Reflection.Assembly.GetExecutingAssembly()
              .GetName().Version?.ToString() ?? "0.1.0.0";

    public SettingsViewModel() => LoadFromDb();

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        App.Settings.MaxScanThreads    = MaxScanThreads;
        App.Settings.MaxDeployThreads  = MaxDeployThreads;
        App.Settings.PingTimeoutMs     = PingTimeoutMs;
        App.Settings.DeployTimeoutSec  = DeployTimeoutSec;
        App.Settings.DefaultSilentArgs = DefaultSilentArgs;
        App.Settings.AdFilter          = AdFilter;
        MessageBox.Show("Settings saved.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        MaxScanThreads    = 20;
        MaxDeployThreads  = 10;
        PingTimeoutMs     = 1500;
        DeployTimeoutSec  = 300;
        DefaultSilentArgs = "/qn /norestart";
        AdFilter          = "(&(objectCategory=computer)(operatingSystem=Windows*))";
    }

    [RelayCommand]
    private void OpenDbFolder()
    {
        var folder = Path.GetDirectoryName(Database.DbPath);
        if (folder is not null && Directory.Exists(folder))
            Process.Start("explorer.exe", folder);
    }

    [RelayCommand]
    private void BackupDb()
    {
        var src = Database.DbPath;
        if (!File.Exists(src)) return;
        var dest = src.Replace(".db", $"_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        File.Copy(src, dest);
        MessageBox.Show($"Backup saved to:\n{dest}", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void LoadFromDb()
    {
        MaxScanThreads    = App.Settings.MaxScanThreads;
        MaxDeployThreads  = App.Settings.MaxDeployThreads;
        PingTimeoutMs     = App.Settings.PingTimeoutMs;
        DeployTimeoutSec  = App.Settings.DeployTimeoutSec;
        DefaultSilentArgs = App.Settings.DefaultSilentArgs;
        AdFilter          = App.Settings.AdFilter;
        DbPath            = Database.DbPath;
    }
}
