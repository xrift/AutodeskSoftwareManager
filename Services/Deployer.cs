using System.IO;
using System.Management;

namespace AutodeskSoftwareManager.Services;

public class DeployResult
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public static class Deployer
{
    private const string RemoteExeName = "AdskInstaller.exe";

    public static async Task<DeployResult> DeployAsync(
        string computerName,
        string installerPath,
        string silentArgs,
        int    timeoutSeconds,
        IProgress<string>? progress  = null,
        CancellationToken  ct        = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                string remoteDest = $@"\\{computerName}\C$\Windows\Temp\{RemoteExeName}";

                progress?.Report("Copying installer...");
                File.Copy(installerPath, remoteDest, overwrite: true);
                ct.ThrowIfCancellationRequested();

                var options = new ConnectionOptions
                {
                    Impersonation    = ImpersonationLevel.Impersonate,
                    EnablePrivileges = true,
                    Timeout          = TimeSpan.FromSeconds(15)
                };
                var scope = new ManagementScope($@"\\{computerName}\root\cimv2", options);
                scope.Connect();

                progress?.Report("Launching installer...");
                using var pc       = new ManagementClass(scope, new ManagementPath("Win32_Process"), null);
                using var inParams = pc.GetMethodParameters("Create");
                inParams["CommandLine"] = $@"C:\Windows\Temp\{RemoteExeName} {silentArgs}";

                using var outParams = pc.InvokeMethod("Create", inParams, null);
                uint rc = (uint)outParams["ReturnValue"];
                if (rc != 0)
                    return new DeployResult { Message = $"WMI Create failed (code {rc})" };

                uint pid = (uint)outParams["ProcessId"];
                progress?.Report("Waiting for installer to complete...");
                WaitForProcess(scope, pid, timeoutSeconds, ct);

                try { File.Delete(remoteDest); } catch { /* best-effort cleanup */ }

                return new DeployResult { Success = true, Message = "Completed successfully" };
            }
            catch (OperationCanceledException)
            {
                return new DeployResult { Message = "Cancelled" };
            }
            catch (Exception ex)
            {
                return new DeployResult { Message = ex.Message };
            }
        }, ct);
    }

    private static void WaitForProcess(ManagementScope scope, uint pid, int timeoutSec, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSec);
        var query    = new ObjectQuery($"SELECT ProcessId FROM Win32_Process WHERE ProcessId={pid}");

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            using var s = new ManagementObjectSearcher(scope, query);
            using var r = s.Get();
            bool running = false;
            foreach (var _ in r) { running = true; break; }
            if (!running) return;
            Thread.Sleep(3000);
        }
    }
}
