namespace AutodeskSoftwareManager.Models;

public class DeploymentPackage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProductFamily { get; set; } = string.Empty;
    public string TargetVersion { get; set; } = string.Empty;
    public string InstallerPath { get; set; } = string.Empty;
    public string SilentArgs { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
