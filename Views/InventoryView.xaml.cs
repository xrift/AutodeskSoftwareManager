using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
        InventoryGrid.ItemsSource = MockInventory.Items;
    }
}

file record MockInventoryItem(string Computer, string OU, string Product, string Year,
    string InstalledVersion, string LatestVersion, string StatusText, string StatusColor,
    bool IsOutdated, string InstallDate, string ScannedAt);

file static class MockInventory
{
    public static readonly MockInventoryItem[] Items =
    [
        new("ENG-WS-001", "ENGINEERING", "AutoCAD",          "2025", "25.0.1.0",   "25.1.0.0",   "Outdated",   "#E67E22", true,  "2025-01-10", "2026-04-26 08:30"),
        new("ENG-WS-001", "ENGINEERING", "Revit",            "2025", "25.0.0.777", "25.0.0.777",  "Up to Date", "#27AE60", false, "2025-01-10", "2026-04-26 08:30"),
        new("ENG-WS-001", "ENGINEERING", "Navisworks Manage","2025", "25.0.0.0",   "25.0.0.0",    "Up to Date", "#27AE60", false, "2025-01-10", "2026-04-26 08:30"),
        new("ENG-WS-001", "ENGINEERING", "Civil 3D",         "2025", "25.0.55.0",  "25.0.55.0",   "Up to Date", "#27AE60", false, "2025-02-01", "2026-04-26 08:30"),
        new("ENG-WS-001", "ENGINEERING", "ReCap Pro",        "2025", "23.1.0.0",   "23.2.0.0",    "Outdated",   "#E67E22", true,  "2025-01-10", "2026-04-26 08:30"),
        new("ENG-WS-002", "ENGINEERING", "Revit",            "2025", "25.0.0.777", "25.0.0.777",  "Up to Date", "#27AE60", false, "2025-01-15", "2026-04-26 08:31"),
        new("ENG-WS-002", "ENGINEERING", "AutoCAD",          "2024", "24.3.0.0",   "25.1.0.0",    "Outdated",   "#E67E22", true,  "2024-02-20", "2026-04-26 08:31"),
        new("ENG-WS-003", "ENGINEERING", "Civil 3D",         "2025", "25.0.55.0",  "25.0.55.0",   "Up to Date", "#27AE60", false, "2025-03-01", "2026-04-26 08:31"),
        new("ENG-WS-003", "ENGINEERING", "InfraWorks",       "2025", "2025.0.0.1", "2025.0.0.1",  "Up to Date", "#27AE60", false, "2025-03-01", "2026-04-26 08:31"),
        new("ARCH-WS-001","ARCHITECTURE","Revit",            "2025", "25.0.0.777", "25.0.0.777",  "Up to Date", "#27AE60", false, "2025-01-20", "2026-04-26 08:29"),
        new("ARCH-WS-001","ARCHITECTURE","AutoCAD",          "2025", "25.0.1.0",   "25.1.0.0",    "Outdated",   "#E67E22", true,  "2025-01-20", "2026-04-26 08:29"),
        new("ARCH-WS-001","ARCHITECTURE","Dynamo",           "2025", "2.18.0",     "2.19.0",      "Outdated",   "#E67E22", true,  "2025-01-20", "2026-04-26 08:29"),
        new("DES-WS-001", "DESIGN",      "3ds Max",          "2025", "27.0.0.0",   "27.0.0.0",    "Up to Date", "#27AE60", false, "2025-01-05", "2026-04-26 08:32"),
        new("DES-WS-001", "DESIGN",      "Arnold Renderer",  "2025", "5.3.1.0",    "5.3.1.0",     "Up to Date", "#27AE60", false, "2025-01-05", "2026-04-26 08:32"),
        new("DES-WS-002", "DESIGN",      "Maya",             "2025", "25.0.0.0",   "25.1.0.0",    "Outdated",   "#E67E22", true,  "2025-01-08", "2026-04-26 08:32"),
        new("ENG-WS-004", "ENGINEERING", "Inventor",         "2024", "28.0.0.0",   "29.0.0.0",    "Outdated",   "#E67E22", true,  "2024-03-15", "2026-04-25 17:12"),
    ];
}
