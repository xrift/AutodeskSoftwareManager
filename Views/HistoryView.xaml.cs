using System.Windows.Controls;

namespace AutodeskSoftwareManager.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
        DataContext = App.HistoryVm;
    }
}
