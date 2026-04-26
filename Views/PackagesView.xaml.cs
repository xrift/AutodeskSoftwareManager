using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class PackagesView : UserControl
{
    public PackagesView()
    {
        InitializeComponent();
        PackageGrid.ItemsSource = MockPkgData.Items;
    }
}

file record MockPkg(string Name, string Family, string Version, string Args, bool IsActive, string Created);

file static class MockPkgData
{
    public static readonly MockPkg[] Items =
    [
        new("AutoCAD 2025 Update 1",  "AutoCAD",  "25.1.0.0",    "/qn /norestart",       true,  "2026-04-10"),
        new("Revit 2025 Update 2",    "Revit",    "25.0.0.900",  "/qn /norestart",       true,  "2026-04-10"),
        new("Civil 3D 2025 Update 1", "Civil 3D", "25.0.60.0",   "/qn /norestart",       true,  "2026-03-15"),
        new("3ds Max 2025.2",         "3ds Max",  "27.2.0.0",    "/silent /norestart",   true,  "2026-03-01"),
        new("Maya 2025.1",            "Maya",     "25.1.0.0",    "/silent",              true,  "2026-02-20"),
        new("Inventor 2025",          "Inventor", "29.0.0.0",    "/qn /norestart",       true,  "2026-01-05"),
        new("AutoCAD 2024 (Legacy)",  "AutoCAD",  "24.0.0.0",    "/qn",                  false, "2025-01-10"),
    ];
}
