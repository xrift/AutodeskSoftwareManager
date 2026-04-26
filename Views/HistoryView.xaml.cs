using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
        HistoryGrid.ItemsSource = MockHistory.Items;
    }
}

file record MockHistoryItem(string Started, string Computer, string OU, string Package,
    string TargetVersion, string ResultText, string ResultColor, bool Success, string Duration, string Message);

file static class MockHistory
{
    public static readonly MockHistoryItem[] Items =
    [
        new("2026-04-26 08:15", "ENG-WS-001",  "ENGINEERING",  "AutoCAD 2025 Update 1",  "25.1.0.0", "Success", "#27AE60", true,  "4m 12s", "Deployment completed successfully"),
        new("2026-04-26 08:15", "ARCH-WS-001", "ARCHITECTURE", "AutoCAD 2025 Update 1",  "25.1.0.0", "Success", "#27AE60", true,  "4m 48s", "Deployment completed successfully"),
        new("2026-04-26 08:15", "ENG-WS-002",  "ENGINEERING",  "AutoCAD 2025 Update 1",  "25.1.0.0", "Failed",  "#E74C3C", false, "0m 12s", "WMI Create failed (code 2) — Access denied"),
        new("2026-04-10 14:30", "DES-WS-001",  "DESIGN",       "3ds Max 2025.2",         "27.2.0.0", "Success", "#27AE60", true,  "7m 05s", "Deployment completed successfully"),
        new("2026-04-10 14:30", "DES-WS-002",  "DESIGN",       "3ds Max 2025.2",         "27.2.0.0", "Success", "#27AE60", true,  "6m 52s", "Deployment completed successfully"),
        new("2026-03-22 11:00", "ENG-WS-003",  "ENGINEERING",  "Civil 3D 2025 Update 1", "25.0.60.0","Success", "#27AE60", true,  "9m 30s", "Deployment completed successfully"),
        new("2026-03-22 11:00", "ENG-WS-004",  "ENGINEERING",  "Civil 3D 2025 Update 1", "25.0.60.0","Failed",  "#E74C3C", false, "1m 45s", "Host unreachable — ping timeout"),
        new("2026-03-01 09:15", "ARCH-WS-002", "ARCHITECTURE", "Revit 2025 Update 2",    "25.0.0.900","Success","#27AE60", true,  "8m 22s", "Deployment completed successfully"),
    ];
}
