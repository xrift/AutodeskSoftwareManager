using CommunityToolkit.Mvvm.ComponentModel;

namespace AutodeskSoftwareManager.Models;

public partial class DeploymentRecord : ObservableObject
{
    public int    Id            { get; set; }
    public int    PackageId     { get; set; }
    public int    ComputerId    { get; set; }
    public string PackageName   { get; set; } = string.Empty;
    public string ComputerName  { get; set; } = string.Empty;
    public string ComputerOU    { get; set; } = string.Empty;
    public string TargetVersion { get; set; } = string.Empty;
    public string StartedAt     { get; set; } = string.Empty;
    public string? FinishedAt   { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultText), nameof(ResultColor), nameof(DurationDisplay))]
    private bool _success;

    [ObservableProperty] private string _message = string.Empty;

    public string ResultText  => Success ? "Success" : "Failed";
    public string ResultColor => Success ? "#27AE60" : "#E74C3C";

    public string DurationDisplay
    {
        get
        {
            if (!DateTime.TryParse(StartedAt,  out var s)) return string.Empty;
            if (!DateTime.TryParse(FinishedAt, out var f)) return string.Empty;
            var d = f - s;
            return $"{(int)d.TotalMinutes}m {d.Seconds:D2}s";
        }
    }

    public string StartedAtDisplay =>
        DateTime.TryParse(StartedAt, out var dt) ? dt.ToString("yyyy-MM-dd  HH:mm") : StartedAt;
}
