using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AutodeskSoftwareManager.Views;

public partial class ComputersView : UserControl
{
    public ComputersView()
    {
        InitializeComponent();
        ComputerGrid.ItemsSource = MockData.Computers;
    }
}

file record MockComputer(string Name, string OU, string Description, string StatusText,
    string StatusColor, string LoggedInUser, int ProductCount, int OutdatedCount,
    string LastScan, string LastSeen);

file static class MockData
{
    public static readonly MockComputer[] Computers =
    [
        new("ENG-WS-001",  "ENGINEERING", "AutoCAD workstation – John Smith",    "Online",  "#27AE60", "jsmith",    6, 1, "2026-04-26 08:30", "2026-04-26 08:30"),
        new("ENG-WS-002",  "ENGINEERING", "Revit workstation – Sarah Connor",    "Online",  "#27AE60", "sconnor",   4, 0, "2026-04-26 08:31", "2026-04-26 08:31"),
        new("ENG-WS-003",  "ENGINEERING", "Civil 3D workstation – Mike Torres",  "Online",  "#27AE60", "mtorres",   5, 2, "2026-04-26 08:31", "2026-04-26 08:31"),
        new("ENG-WS-004",  "ENGINEERING", "Inventor workstation",                "Offline", "#E74C3C", "",          3, 0, "2026-04-25 17:12", "2026-04-25 17:14"),
        new("ARCH-WS-001", "ARCHITECTURE","Revit Architecture – Lisa Park",      "Online",  "#27AE60", "lpark",     7, 3, "2026-04-26 08:29", "2026-04-26 08:29"),
        new("ARCH-WS-002", "ARCHITECTURE","AutoCAD LT – David Osei",             "Online",  "#27AE60", "dosei",     2, 0, "2026-04-26 08:29", "2026-04-26 08:29"),
        new("ARCH-WS-003", "ARCHITECTURE","Unassigned",                          "Offline", "#E74C3C", "",          0, 0, "Never",            "2026-04-20 09:00"),
        new("DES-WS-001",  "DESIGN",      "3ds Max workstation – Kim Nguyen",    "Online",  "#27AE60", "knguyen",   3, 1, "2026-04-26 08:32", "2026-04-26 08:32"),
        new("DES-WS-002",  "DESIGN",      "Maya workstation – Ray Patel",        "Online",  "#27AE60", "rpatel",    4, 2, "2026-04-26 08:32", "2026-04-26 08:32"),
        new("IT-SRV-001",  "IT",          "Software deployment server",          "Online",  "#27AE60", "",          1, 0, "2026-04-26 08:28", "2026-04-26 08:28"),
    ];
}
