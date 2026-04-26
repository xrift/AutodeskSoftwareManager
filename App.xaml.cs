using System.IO;
using System.Windows;
using AutodeskSoftwareManager.Data;
using AutodeskSoftwareManager.Models;
using AutodeskSoftwareManager.Services;
using AutodeskSoftwareManager.ViewModels;

namespace AutodeskSoftwareManager;

public partial class App : Application
{
    public static DatabaseService          Db       { get; private set; } = null!;
    public static SettingsService          Settings { get; private set; } = null!;
    public static IReadOnlyList<ProductCatalog> Catalog { get; private set; } = [];

    public static MainViewModel       MainVm      { get; private set; } = null!;
    public static ComputersViewModel  ComputersVm { get; private set; } = null!;
    public static InventoryViewModel  InventoryVm { get; private set; } = null!;
    public static DeployViewModel     DeployVm    { get; private set; } = null!;
    public static PackagesViewModel   PackagesVm  { get; private set; } = null!;
    public static HistoryViewModel    HistoryVm   { get; private set; } = null!;
    public static SettingsViewModel   SettingsVm  { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutodeskSoftwareManager", "manager.db");

        Database.Initialize(dbPath);
        Db       = new DatabaseService();
        Settings = new SettingsService(Db);
        Catalog  = Db.GetCatalog().ToList();

        MainVm      = new MainViewModel();
        ComputersVm = new ComputersViewModel();
        InventoryVm = new InventoryViewModel();
        PackagesVm  = new PackagesViewModel();
        HistoryVm   = new HistoryViewModel();
        SettingsVm  = new SettingsViewModel();
        DeployVm    = new DeployViewModel();

        new MainWindow().Show();
    }
}
