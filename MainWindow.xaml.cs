using System.Windows;
using System.Windows.Controls;

namespace AutodeskSoftwareManager;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (ViewComputers is null) return; // called before InitializeComponent finishes

        ViewComputers.Visibility  = Visibility.Collapsed;
        ViewInventory.Visibility  = Visibility.Collapsed;
        ViewDeploy.Visibility     = Visibility.Collapsed;
        ViewPackages.Visibility   = Visibility.Collapsed;
        ViewHistory.Visibility    = Visibility.Collapsed;
        ViewSettings.Visibility   = Visibility.Collapsed;

        if (sender == NavComputers) ViewComputers.Visibility  = Visibility.Visible;
        else if (sender == NavInventory) ViewInventory.Visibility  = Visibility.Visible;
        else if (sender == NavDeploy)    ViewDeploy.Visibility     = Visibility.Visible;
        else if (sender == NavPackages)  ViewPackages.Visibility   = Visibility.Visible;
        else if (sender == NavHistory)   ViewHistory.Visibility    = Visibility.Visible;
        else if (sender == NavSettings)  ViewSettings.Visibility   = Visibility.Visible;
    }
}
