namespace AutodeskSoftwareManager.Models;

public enum ScanStatus
{
    Pending,
    Scanning,
    Online,
    Offline,
    Error
}

public enum DeployStatus
{
    Pending,
    Queued,
    Copying,
    Launching,
    Running,
    Success,
    Failed,
    Cancelled
}
