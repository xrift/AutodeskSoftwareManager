namespace AutodeskSoftwareManager.Models;

public class ProductCatalog
{
    public int Id { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string RegistryHint { get; set; } = string.Empty;   // substring matched against DisplayName
    public string LatestVersion { get; set; } = string.Empty;
}
