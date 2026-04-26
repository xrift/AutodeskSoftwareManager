using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
        DataContext = App.InventoryVm;
    }
}
