using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class DeployView : UserControl
{
    public DeployView()
    {
        InitializeComponent();
        DataContext = App.DeployVm;
    }
}
