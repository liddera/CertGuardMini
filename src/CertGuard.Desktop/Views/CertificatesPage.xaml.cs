using System.Windows;
using System.Windows.Controls;
using CertGuard.Core.Models;
using CertGuard.Desktop.ViewModels;

namespace CertGuard.Desktop.Views;

public partial class CertificatesPage : UserControl
{
    public CertificatesPage()
    {
        InitializeComponent();
        Loaded += CertificatesPage_Loaded;
    }

    private async void CertificatesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CertificatesViewModel vm)
        {
            await vm.LoadCertificatesCommand.ExecuteAsync(null);
        }
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Certificado certificado &&
            DataContext is CertificatesViewModel vm)
        {
            _ = vm.ActivateAsync(certificado);
        }
    }
}
