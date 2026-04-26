using System.Management;
using System.Net.NetworkInformation;
using AutodeskSoftwareManager.Models;
using Microsoft.Win32;

namespace AutodeskSoftwareManager.Services;

public class ScanResult
{
    public bool IsOnline { get; set; }
    public string LoggedInUser { get; set; } = string.Empty;
    public List<InstalledProduct> Products { get; set; } = [];
    public string? Error { get; set; }
}

public static class InventoryScanner
{
    private static readonly string[] UninstallPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
    ];

    public static async Task<ScanResult> ScanAsync(
        int computerId,
        string computerName,
        IReadOnlyList<ProductCatalog> catalog,
        int pingTimeoutMs = 1500)
    {
        var result = new ScanResult();

        result.IsOnline = await PingAsync(computerName, pingTimeoutMs);
        if (!result.IsOnline) return result;

        await Task.Run(() =>
        {
            try
            {
                result.LoggedInUser = GetLoggedInUser(computerName);
            }
            catch { /* non-fatal */ }

            try
            {
                result.Products = ScanRegistry(computerId, computerName, catalog);
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }
        });

        return result;
    }

    private static List<InstalledProduct> ScanRegistry(
        int computerId, string computerName, IReadOnlyList<ProductCatalog> catalog)
    {
        var products = new List<InstalledProduct>();

        using var hklm = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computerName);

        foreach (var path in UninstallPaths)
        {
            using var uninstall = hklm.OpenSubKey(path);
            if (uninstall is null) continue;

            foreach (var subKeyName in uninstall.GetSubKeyNames())
            {
                using var sub = uninstall.OpenSubKey(subKeyName);
                if (sub is null) continue;

                var publisher   = sub.GetValue("Publisher")?.ToString() ?? string.Empty;
                var displayName = sub.GetValue("DisplayName")?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(displayName)) continue;
                if (!IsAutodesk(publisher, displayName)) continue;

                var version       = sub.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
                var installDate   = sub.GetValue("InstallDate")?.ToString() ?? string.Empty;
                var installLoc    = sub.GetValue("InstallLocation")?.ToString() ?? string.Empty;

                var match = MatchCatalog(displayName, catalog);

                products.Add(new InstalledProduct
                {
                    ComputerId      = computerId,
                    CatalogId       = match?.Id,
                    DisplayName     = displayName,
                    ProductFamily   = match?.ProductFamily ?? string.Empty,
                    DisplayVersion  = version,
                    LatestVersion   = match?.LatestVersion ?? string.Empty,
                    InstallDate     = installDate,
                    InstallLocation = installLoc,
                    UninstallKey    = subKeyName,
                    IsOutdated      = IsOutdated(version, match?.LatestVersion)
                });
            }
        }

        // Deduplicate by DisplayName (same product can appear in both 32 and 64-bit paths)
        return products
            .GroupBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(p => p.DisplayVersion).First())
            .OrderBy(p => p.DisplayName)
            .ToList();
    }

    private static bool IsAutodesk(string publisher, string displayName) =>
        publisher.Contains("Autodesk", StringComparison.OrdinalIgnoreCase) ||
        displayName.Contains("Autodesk", StringComparison.OrdinalIgnoreCase);

    private static ProductCatalog? MatchCatalog(string displayName, IReadOnlyList<ProductCatalog> catalog)
    {
        ProductCatalog? bestMatch = null;
        int bestLen = 0;

        foreach (var entry in catalog)
        {
            if (string.IsNullOrEmpty(entry.RegistryHint)) continue;
            if (!displayName.Contains(entry.RegistryHint, StringComparison.OrdinalIgnoreCase)) continue;
            if (entry.RegistryHint.Length > bestLen)
            {
                bestMatch = entry;
                bestLen   = entry.RegistryHint.Length;
            }
        }

        return bestMatch;
    }

    private static bool IsOutdated(string installed, string? latest)
    {
        if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest)) return false;
        if (!Version.TryParse(installed, out var v1)) return false;
        if (!Version.TryParse(latest,    out var v2)) return false;
        return v1 < v2;
    }

    private static string GetLoggedInUser(string computerName)
    {
        var options = new ConnectionOptions
        {
            Impersonation    = ImpersonationLevel.Impersonate,
            EnablePrivileges = true,
            Timeout          = TimeSpan.FromSeconds(10)
        };
        var scope = new ManagementScope($@"\\{computerName}\root\cimv2", options);
        scope.Connect();

        var users = new List<string>();
        using var searcher = new ManagementObjectSearcher(scope,
            new ObjectQuery("SELECT Handle FROM Win32_Process WHERE Name='explorer.exe'"));
        using var results = searcher.Get();

        foreach (ManagementObject proc in results)
        {
            using var outParams = proc.InvokeMethod("GetOwner", null, null);
            var user = outParams?["User"]?.ToString();
            if (!string.IsNullOrEmpty(user)) users.Add(user);
        }

        return users.Count > 0
            ? string.Join(", ", users.Distinct(StringComparer.OrdinalIgnoreCase))
            : string.Empty;
    }

    private static async Task<bool> PingAsync(string host, int timeoutMs)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, timeoutMs);
            return reply.Status == IPStatus.Success;
        }
        catch { return false; }
    }
}
