using System.Windows;

namespace CertGuard.Desktop.Views;

public partial class ExpiryDialog : Window
{
    public ExpiryDialog()
    {
        InitializeComponent();
    }

    private void Extend_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
