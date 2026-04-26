using System.Windows;
using System.Windows.Input;

namespace AutodeskSoftwareManager.Views;

public partial class AddComputerDialog : Window
{
    public string ComputerName { get; private set; } = string.Empty;

    public AddComputerDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e) => TryAccept();
    private void NameBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) TryAccept(); }

    private void TryAccept()
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorText.Text       = "Please enter a computer name.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        ComputerName = name.ToUpperInvariant();
        DialogResult = true;
    }
}
