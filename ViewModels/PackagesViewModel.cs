using System.Collections.ObjectModel;
using AutodeskSoftwareManager.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AutodeskSoftwareManager.ViewModels;

public partial class PackagesViewModel : ObservableObject
{
    public ObservableCollection<DeploymentPackage> Packages { get; } = [];

    [ObservableProperty] private DeploymentPackage? _selectedPackage;

    // ── Form fields ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _formName          = string.Empty;
    [ObservableProperty] private string _formFamily        = "AutoCAD";
    [ObservableProperty] private string _formVersion       = string.Empty;
    [ObservableProperty] private string _formInstallerPath = string.Empty;
    [ObservableProperty] private string _formSilentArgs    = string.Empty;
    [ObservableProperty] private string _formNotes         = string.Empty;
    [ObservableProperty] private bool   _formIsActive      = true;
    [ObservableProperty] private bool   _isEditing;
    [ObservableProperty] private string _formTitle         = "ADD PACKAGE";

    private int _editingId = -1;

    public static readonly string[] ProductFamilies =
    [
        "AutoCAD","AutoCAD LT","AutoCAD Architecture","AutoCAD Civil 3D",
        "AutoCAD Electrical","AutoCAD Mechanical","AutoCAD MEP","AutoCAD Plant 3D",
        "Revit","Inventor","Inventor LT","3ds Max","Maya",
        "Navisworks Manage","Navisworks Simulate","InfraWorks",
        "ReCap Pro","Robot Structural Analysis","Advance Steel",
        "Vault Professional","Fusion 360","Arnold Renderer","Other"
    ];

    public PackagesViewModel()
    {
        FormSilentArgs = App.Settings.DefaultSilentArgs;
        LoadFromDb();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void NewPackage()
    {
        _editingId     = -1;
        FormTitle      = "ADD PACKAGE";
        FormName          = string.Empty;
        FormFamily        = "AutoCAD";
        FormVersion       = string.Empty;
        FormInstallerPath = string.Empty;
        FormSilentArgs    = App.Settings.DefaultSilentArgs;
        FormNotes         = string.Empty;
        FormIsActive      = true;
        IsEditing         = true;
    }

    [RelayCommand]
    private void EditPackage()
    {
        if (SelectedPackage is null) return;
        _editingId        = SelectedPackage.Id;
        FormTitle         = "EDIT PACKAGE";
        FormName          = SelectedPackage.Name;
        FormFamily        = SelectedPackage.ProductFamily;
        FormVersion       = SelectedPackage.TargetVersion;
        FormInstallerPath = SelectedPackage.InstallerPath;
        FormSilentArgs    = SelectedPackage.SilentArgs;
        FormNotes         = SelectedPackage.Notes;
        FormIsActive      = SelectedPackage.IsActive;
        IsEditing         = true;
    }

    [RelayCommand]
    private void SavePackage()
    {
        if (string.IsNullOrWhiteSpace(FormName) || string.IsNullOrWhiteSpace(FormInstallerPath))
            return;

        var pkg = new DeploymentPackage
        {
            Name          = FormName.Trim(),
            ProductFamily = FormFamily,
            TargetVersion = FormVersion.Trim(),
            InstallerPath = FormInstallerPath.Trim(),
            SilentArgs    = FormSilentArgs.Trim(),
            Notes         = FormNotes.Trim(),
            IsActive      = FormIsActive,
            CreatedAt     = DateTime.Now
        };

        if (_editingId < 0)
        {
            pkg.Id = App.Db.InsertPackage(pkg);
            Packages.Add(pkg);
        }
        else
        {
            pkg.Id = _editingId;
            App.Db.UpdatePackage(pkg);
            var idx = Packages.IndexOf(Packages.First(p => p.Id == _editingId));
            Packages[idx] = pkg;
        }

        IsEditing = false;
        App.DeployVm.RefreshPackages();
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private void DeletePackage()
    {
        if (SelectedPackage is null) return;
        App.Db.DeletePackage(SelectedPackage.Id);
        Packages.Remove(SelectedPackage);
        App.DeployVm.RefreshPackages();
    }

    [RelayCommand]
    private void BrowseInstaller()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select Installer",
            Filter = "Executables (*.exe;*.msi)|*.exe;*.msi|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
            FormInstallerPath = dlg.FileName;
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private void LoadFromDb()
    {
        Packages.Clear();
        foreach (var p in App.Db.GetPackages())
            Packages.Add(p);
    }
}
