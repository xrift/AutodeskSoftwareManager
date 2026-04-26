using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class DeployView : UserControl
{
    public DeployView()
    {
        InitializeComponent();
        PackageList.ItemsSource = MockPackages.Items;
        PackageList.SelectedIndex = 0;
        DeployGrid.ItemsSource = MockDeployTargets.Items;
    }
}

file record MockPackage(string Name, string ProductFamily, string Version);
file record MockDeployTarget(string Computer, string OU, string CurrentVersion, string TargetVersion,
    string StatusText, string StatusColor, bool IsOutdated, bool IsSelected, string LoggedInUser, string LastDeploy);

file static class MockPackages
{
    public static readonly MockPackage[] Items =
    [
        new("AutoCAD 2025 Update 1",  "AutoCAD",   "v25.1.0.0"),
        new("Revit 2025 Update 2",    "Revit",     "v25.0.0.900"),
        new("Civil 3D 2025 Update 1", "Civil 3D",  "v25.0.60.0"),
        new("3ds Max 2025.2",         "3ds Max",   "v27.2.0.0"),
        new("Maya 2025.1",            "Maya",      "v25.1.0.0"),
        new("Inventor 2025",          "Inventor",  "v29.0.0.0"),
    ];
}

file static class MockDeployTargets
{
    public static readonly MockDeployTarget[] Items =
    [
        new("ENG-WS-001", "ENGINEERING", "25.0.1.0", "25.1.0.0", "Outdated",   "#E67E22", true,  true,  "jsmith",   "2026-02-10"),
        new("ENG-WS-002", "ENGINEERING", "24.3.0.0", "25.1.0.0", "Outdated",   "#E67E22", true,  true,  "sconnor",  "Never"),
        new("ARCH-WS-001","ARCHITECTURE","25.0.1.0", "25.1.0.0", "Outdated",   "#E67E22", true,  true,  "lpark",    "2026-01-15"),
        new("ARCH-WS-002","ARCHITECTURE","25.1.0.0", "25.1.0.0", "Up to Date", "#27AE60", false, false, "dosei",    "2026-04-01"),
        new("DES-WS-001", "DESIGN",      "25.1.0.0", "25.1.0.0", "Up to Date", "#27AE60", false, false, "knguyen",  "2026-04-01"),
    ];
}
