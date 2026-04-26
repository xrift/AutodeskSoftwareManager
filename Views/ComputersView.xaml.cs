using System.Windows;
using System.Windows.Controls;
using AutodeskSoftwareManager.ViewModels;

namespace AutodeskSoftwareManager.Views;

public partial class ComputersView : UserControl
{
    public ComputersView()
    {
        InitializeComponent();
        DataContext = App.ComputersVm;
    }

    private void AddComputer_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddComputerDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
            App.ComputersVm.AddComputerCommand.Execute(dlg.ComputerName);
    }
}
