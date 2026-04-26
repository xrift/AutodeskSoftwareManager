using CommunityToolkit.Mvvm.ComponentModel;

namespace AutodeskSoftwareManager.Models;

public partial class InstalledProduct : ObservableObject
{
    public int    Id              { get; set; }
    public int    ComputerId      { get; set; }
    public int?   CatalogId       { get; set; }
    public string ComputerName    { get; set; } = string.Empty;
    public string ComputerOU      { get; set; } = string.Empty;
    public string DisplayName     { get; set; } = string.Empty;
    public string ProductFamily   { get; set; } = string.Empty;
    public string DisplayVersion  { get; set; } = string.Empty;
    public string LatestVersion   { get; set; } = string.Empty;
    public string InstallDate     { get; set; } = string.Empty;
    public string InstallLocation { get; set; } = string.Empty;
    public string UninstallKey    { get; set; } = string.Empty;
    public string ScannedAt       { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(StatusColor))]
    private bool _isOutdated;

    public string StatusText  => IsOutdated ? "Outdated"  : "Up to Date";
    public string StatusColor => IsOutdated ? "#E67E22"   : "#27AE60";

    public string ScannedAtDisplay =>
        DateTime.TryParse(ScannedAt, out var dt) ? dt.ToString("yyyy-MM-dd  HH:mm") : ScannedAt;
}
