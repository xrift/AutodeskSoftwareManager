using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class PackagesView : UserControl
{
    public PackagesView()
    {
        InitializeComponent();
        DataContext = App.PackagesVm;
    }
}
