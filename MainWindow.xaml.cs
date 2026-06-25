using System.Windows;
using System.Windows.Media;
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
        _broker.LoadSimulatedCertificate("Certificado Teste - Empresa ABC");
        txtCertInfo.Text = $"Certificado: {_broker.CurrentCertificate?.DisplayName}\n" +
                           $"Thumbprint: {_broker.CurrentCertificate?.Thumbprint}\n" +
                           $"Domínios permitidos: {_broker.CurrentCertificate?.AllowedDomains.Count}\n" +
                           $"Domínios bloqueados: {_broker.CurrentCertificate?.BlockedDomains.Count}";

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

    private async void btnStartProxy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            btnStartProxy.IsEnabled = false;

            _proxy = new ProxyService(_broker);
            _proxy.Log += (s, msg) => Dispatcher.Invoke(() => AddLog(msg));
            _proxy.RequestBlocked += (s, domain) => Dispatcher.Invoke(() =>
            {
                AddLog($"🛡️ BLOQUEIO: {domain}");
                UpdateStatus("BLOQUEIO DETECTADO", Brushes.Red);
            });
            _proxy.RequestAllowed += (s, domain) => Dispatcher.Invoke(() =>
            {
                AddLog($"✅ LIBERADO: {domain}");
            });

            await _proxy.StartAsync();

            UpdateStatus("PROXY ATIVO - Porta " + _proxy.Port, Brushes.LimeGreen);
            btnStartProxy.IsEnabled = false;
            btnStopProxy.IsEnabled = true;

            MessageBox.Show(
                $"Proxy ativo na porta {_proxy.Port}!\n\n" +
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
            MessageBox.Show($"Erro ao iniciar proxy:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            btnStartProxy.IsEnabled = true;
        }
    }

    private void btnStopProxy_Click(object sender, RoutedEventArgs e)
    {
        _proxy?.Stop();
        _proxy?.Dispose();
        _proxy = null;

        _broker.Unload();

        UpdateStatus("PROXY INATIVO", Brushes.Gray);
        btnStartProxy.IsEnabled = true;
        btnStopProxy.IsEnabled = false;

        AddLog("Proxy parado e certificado descarregado");
    }

    private void btnAddBlocked_Click(object sender, RoutedEventArgs e)
    {
        var domain = txtNewDomain.Text.Trim();
        if (string.IsNullOrEmpty(domain)) return;

        _broker.AddDomainRule(domain, domain, isBlocked: true);
        LoadDomainRules();
        txtNewDomain.Text = "";

        AddLog($"Domínio BLOQUEADO adicionado: {domain}");
    }

    private void btnAddAllowed_Click(object sender, RoutedEventArgs e)
    {
        var domain = txtNewDomain.Text.Trim();
        if (string.IsNullOrEmpty(domain)) return;

        _broker.AddDomainRule(domain, domain, isBlocked: false);
        LoadDomainRules();
        txtNewDomain.Text = "";

        AddLog($"Domínio PERMITIDO adicionado: {domain}");
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
