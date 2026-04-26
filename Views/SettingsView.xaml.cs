using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = App.SettingsVm;
    }
}
