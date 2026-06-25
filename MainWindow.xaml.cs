using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using CertGuardMini.Models;
using CertGuardMini.Services;

namespace CertGuardMini;

public partial class MainWindow : Window
{
    private readonly CertBrokerService _broker = new();
    private ProxyService? _proxy;

    public MainWindow()
    {
        InitializeComponent();
        LoadDefaultRules();
    }

    private void LoadDefaultRules()
    {
        LoadDomainRules();
    }

    private void LoadDomainRules()
    {
        lstDomains.Items.Clear();
        foreach (var rule in _broker.DomainRules)
        {
            var prefix = rule.IsBlocked ? "❌" : "✅";
            lstDomains.Items.Add($"{prefix} {rule.Domain}");
        }
    }

    private void btnLoadPfx_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Certificados PFX|*.pfx;*.p12|Todos os arquivos|*.*",
            Title = "Selecionar certificado digital"
        };

        if (dialog.ShowDialog() == true)
        {
            var password = txtPassword.Password;
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Digite a senha do certificado.", "Senha necessária",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _broker.LoadFromPfxFile(dialog.FileName, password);
                UpdateCertInfo();
                LoadDomainRules();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar PFX:\n{ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void btnLoadSimulated_Click(object sender, RoutedEventArgs e)
    {
        _broker.LoadSimulatedCertificate("Certificado Teste - Empresa ABC");
        UpdateCertInfo();
        LoadDomainRules();
    }

    private void UpdateCertInfo()
    {
        var cert = _broker.Certificate;
        if (cert is not null)
        {
            txtCertInfo.Text = $"Tipo: REAL (PFX)\n" +
                               $"Titular: {cert.SubjectName.Name}\n" +
                               $"Thumbprint: {cert.Thumbprint[..16]}...\n" +
                               $"Validade: {cert.NotAfter:dd/MM/yyyy}\n" +
                               $"Domínios: {_broker.DomainRules.Count}";
            txtCertInfo.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
        }
        else if (_broker.CurrentCertificate is not null)
        {
            txtCertInfo.Text = $"Tipo: SIMULADO\n" +
                               $"Nome: {_broker.CurrentCertificate.DisplayName}\n" +
                               $"Thumbprint: {_broker.CurrentCertificate.Thumbprint}\n" +
                               $"Domínios: {_broker.DomainRules.Count}";
            txtCertInfo.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00));
        }
        else
        {
            txtCertInfo.Text = "Nenhum certificado carregado";
            txtCertInfo.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
        }
    }

    private async void btnStartProxy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            btnStartProxy.IsEnabled = false;

            _proxy = new ProxyService(_broker);
            _proxy.Log += (s, msg) => Dispatcher.Invoke(() => AddLog(msg));
            _proxy.RequestBlocked += (s, domain) => Dispatcher.Invoke(() =>
            {
                AddLog($"BLOQUEIO: {domain}");
                UpdateStatus("BLOQUEIO DETECTADO", Brushes.Red);
            });
            _proxy.RequestAllowed += (s, domain) => Dispatcher.Invoke(() =>
            {
                AddLog($"LIBERADO: {domain}");
            });
            _proxy.CertificateUsed += (s, info) => Dispatcher.Invoke(() =>
            {
                AddLog($"CERTIFICADO APLICADO: {info}");
            });

            await _proxy.StartAsync();

            var certType = _broker.Certificate is not null ? "REAL" : "SIMULADO";
            UpdateStatus($"PROXY ATIVO ({certType}) - Porta {_proxy.Port}", Brushes.LimeGreen);
            btnStartProxy.IsEnabled = false;
            btnStopProxy.IsEnabled = true;

            MessageBox.Show(
                $"Proxy ativo na porta {_proxy.Port}!\n" +
                $"Certificado: {certType}\n\n" +
                "Teste agora:\n" +
                "1. Abra o navegador\n" +
                "2. Acesse https://download.dfe.sefin.ro.gov.br\n" +
                "3. Deve ser BLOQUEADO\n\n" +
                "4. Acesse https://google.com\n" +
                "5. Deve ser PERMITIDO",
                "CertGuard Mini",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao iniciar proxy:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
            btnStartProxy.IsEnabled = true;
        }
    }

    private void btnStopProxy_Click(object sender, RoutedEventArgs e)
    {
        _proxy?.Stop();
        _proxy?.Dispose();
        _proxy = null;

        UpdateStatus("PROXY INATIVO", Brushes.Gray);
        btnStartProxy.IsEnabled = true;
        btnStopProxy.IsEnabled = false;

        AddLog("Proxy parado");
    }

    private void btnAddBlocked_Click(object sender, RoutedEventArgs e)
    {
        var domain = txtNewDomain.Text.Trim();
        if (string.IsNullOrEmpty(domain)) return;

        _broker.AddDomainRule(domain, domain, isBlocked: true);
        LoadDomainRules();
        txtNewDomain.Text = "";
        AddLog($"Domínio BLOQUEADO: {domain}");
    }

    private void btnAddAllowed_Click(object sender, RoutedEventArgs e)
    {
        var domain = txtNewDomain.Text.Trim();
        if (string.IsNullOrEmpty(domain)) return;

        _broker.AddDomainRule(domain, domain, isBlocked: false);
        LoadDomainRules();
        txtNewDomain.Text = "";
        AddLog($"Domínio PERMITIDO: {domain}");
    }

    private void btnRemoveDomain_Click(object sender, RoutedEventArgs e)
    {
        if (lstDomains.SelectedItem is null) return;

        var selected = lstDomains.SelectedItem.ToString() ?? "";
        var domain = selected.Replace("✅ ", "").Replace("❌ ", "").Trim();

        _broker.RemoveDomainRule(domain);
        LoadDomainRules();
        AddLog($"Domínio removido: {domain}");
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        txtLog.AppendText($"[{timestamp}] {message}\n");
        txtLog.ScrollToEnd();
    }

    private void UpdateStatus(string text, Brush color)
    {
        txtStatus.Text = text;
        txtStatus.Foreground = color;
    }
}
