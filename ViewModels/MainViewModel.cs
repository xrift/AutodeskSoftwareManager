using CommunityToolkit.Mvvm.ComponentModel;

namespace AutodeskSoftwareManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string  _statusText    = "Ready";
    [ObservableProperty] private string  _busyMessage   = string.Empty;
    [ObservableProperty] private bool    _isBusy;
    [ObservableProperty] private int     _scanProgress;
    [ObservableProperty] private int     _onlineCount;
    [ObservableProperty] private int     _totalCount;

    public void RefreshCounts()
    {
        var (total, online, _, _, _) = App.Db.GetComputerStats();
        TotalCount  = total;
        OnlineCount = online;
        StatusText  = total == 0
            ? "Ready"
            : $"Ready  ·  Last refreshed: {DateTime.Now:yyyy-MM-dd  hh:mm tt}";
    }

    public void SetBusy(bool busy, string message = "", int progress = 0)
    {
        IsBusy       = busy;
        BusyMessage  = message;
        ScanProgress = progress;
        if (!busy) BusyMessage = string.Empty;
    }
}
