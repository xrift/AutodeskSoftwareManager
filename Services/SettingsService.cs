namespace AutodeskSoftwareManager.Services;

public class SettingsService(DatabaseService db)
{
    public int MaxScanThreads
    {
        get => int.TryParse(db.GetSetting("MaxScanThreads"), out var v) ? v : 20;
        set => db.SetSetting("MaxScanThreads", value.ToString());
    }

    public int MaxDeployThreads
    {
        get => int.TryParse(db.GetSetting("MaxDeployThreads"), out var v) ? v : 10;
        set => db.SetSetting("MaxDeployThreads", value.ToString());
    }

    public int PingTimeoutMs
    {
        get => int.TryParse(db.GetSetting("PingTimeoutMs"), out var v) ? v : 1500;
        set => db.SetSetting("PingTimeoutMs", value.ToString());
    }

    public int DeployTimeoutSec
    {
        get => int.TryParse(db.GetSetting("DeployTimeoutSec"), out var v) ? v : 300;
        set => db.SetSetting("DeployTimeoutSec", value.ToString());
    }

    public string DefaultSilentArgs
    {
        get => db.GetSetting("DefaultSilentArgs", "/qn /norestart");
        set => db.SetSetting("DefaultSilentArgs", value);
    }

    public string AdFilter
    {
        get => db.GetSetting("AdFilter", "(&(objectCategory=computer)(operatingSystem=Windows*))");
        set => db.SetSetting("AdFilter", value);
    }
}
