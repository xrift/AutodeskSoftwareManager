using CommunityToolkit.Mvvm.ComponentModel;

namespace AutodeskSoftwareManager.Models;

public partial class Computer : ObservableObject
{
    // ── DB-persisted (set by Dapper) ─────────────────────────────────────────
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OU { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(StatusColor))]
    private bool _isOnline;

    [ObservableProperty] private string _loggedInUser = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastSeenDisplay))]
    private string? _lastSeen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastScanDisplay))]
    private string? _lastScan;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutdated))]
    private int _productCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutdated), nameof(OutdatedDisplay))]
    private int _outdatedCount;

    // ── Transient UI state (not persisted) ───────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(StatusColor))]
    private bool _isScanning;

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isHidden;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // ── Computed display helpers ─────────────────────────────────────────────
    public string StatusText  => IsScanning ? "Scanning" : (IsOnline ? "Online" : "Offline");
    public string StatusColor => IsScanning ? "#3498DB"  : (IsOnline ? "#27AE60" : "#E74C3C");

    public string LastSeenDisplay => TryFormat(LastSeen);
    public string LastScanDisplay => string.IsNullOrEmpty(LastScan) ? "Never" : TryFormat(LastScan);

    public bool   HasOutdated    => OutdatedCount > 0;
    public string OutdatedDisplay => OutdatedCount > 0 ? OutdatedCount.ToString() : string.Empty;

    private static string TryFormat(string? iso) =>
        DateTime.TryParse(iso, out var dt) ? dt.ToString("yyyy-MM-dd  HH:mm") : string.Empty;
}
