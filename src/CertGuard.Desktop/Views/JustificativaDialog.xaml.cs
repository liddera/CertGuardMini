using System.Windows;

namespace CertGuard.Desktop.Views;

public partial class JustificativaDialog : Window
{
    public string Justification => JustificationBox.Text;

    public JustificativaDialog()
    {
        InitializeComponent();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(JustificationBox.Text))
        {
            MessageBox.Show("Informe uma justificativa.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
