using System.Net;
using Unobtanium.Web.Proxy;
using Unobtanium.Web.Proxy.EventArguments;
using Unobtanium.Web.Proxy.Models;

namespace CertGuardMini.Services;

public class ProxyService : IDisposable
{
    private readonly ProxyServer _proxyServer;
    private readonly CertBrokerService _broker;
    private ExplicitProxyEndPoint? _endpoint;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public int Port { get; }

    public event EventHandler<string>? Log;
    public event EventHandler<string>? RequestBlocked;
    public event EventHandler<string>? RequestAllowed;

    public ProxyService(CertBrokerService broker, int port = 8888)
    {
        _broker = broker;
        _proxyServer = new ProxyServer("CertGuardMini", "1.0.0");
        Port = port;
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        try
        {
            _proxyServer.BeforeRequest += OnBeforeRequest;
            _proxyServer.AfterResponse += OnAfterResponse;

            _endpoint = new ExplicitProxyEndPoint(IPAddress.Any, Port, true);
            _proxyServer.AddEndPoint(_endpoint);

            _proxyServer.CertificateManager.EnsureRootCertificate();
            _proxyServer.Start();

            _proxyServer.SetAsSystemProxy(_endpoint, ProxyProtocolType.AllHttp);

            _isRunning = true;
            Log?.Invoke(this, $"Proxy iniciado na porta {Port}");
        }
        catch (Exception ex)
        {
            Log?.Invoke(this, $"Erro ao iniciar proxy: {ex.Message}");
            throw;
        }
    }

    private async Task OnBeforeRequest(object sender, SessionEventArgs e)
    {
        var host = e.HttpClient.Request.Host?.ToLower() ?? "";
        var url = e.HttpClient.Request.Url?.ToLower() ?? "";

        Log?.Invoke(this, $">>> Requisição: {host}{e.HttpClient.Request.RequestUri?.PathAndQuery}");

        if (!_broker.IsDomainAllowed(host))
        {
            e.HttpClient.Response.StatusCode = HttpStatusCode.Forbidden;
            e.HttpClient.Response.BodyString = GenerateBlockedPage(host);

            RequestBlocked?.Invoke(this, host);
            Log?.Invoke(this, $"❌ BLOQUEADO: {host}");
            return;
        }

        RequestAllowed?.Invoke(this, host);
        Log?.Invoke(this, $"✅ PERMITIDO: {host}");

        await Task.CompletedTask;
    }

    private Task OnAfterResponse(object sender, SessionEventArgs e)
    {
        return Task.CompletedTask;
    }

    private string GenerateBlockedPage(string domain)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>CertGuard Mini - Acesso Bloqueado</title>
    <style>
        body {{ font-family: 'Segoe UI', sans-serif; background: #1a1a2e; color: #e0e0e0;
               display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }}
        .card {{ background: #16213e; border: 2px solid #e94560; border-radius: 12px;
                padding: 40px; text-align: center; max-width: 500px; }}
        .icon {{ font-size: 64px; margin-bottom: 20px; }}
        h1 {{ color: #e94560; margin: 0 0 10px 0; font-size: 24px; }}
        .domain {{ background: #0f3460; padding: 10px; border-radius: 6px;
                   font-family: monospace; color: #e94560; margin: 15px 0; }}
        p {{ color: #a0a0a0; line-height: 1.6; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #555; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='icon'>🛡️</div>
        <h1>Acesso Bloqueado</h1>
        <p>O domínio abaixo não está na lista de permitidos:</p>
        <div class='domain'>{domain}</div>
        <p>CertGuard Mini está protegendo seu tráfego.<br/>
           Entre em contato com o administrador para solicitar acesso.</p>
        <div class='footer'>CertGuard Mini v1.0.0 - Protótipo</div>
    </div>
</body>
</html>";
    }

    public void Stop()
    {
        if (!_isRunning) return;

        try
        {
            _proxyServer.Stop();

            _proxyServer.BeforeRequest -= OnBeforeRequest;
            _proxyServer.AfterResponse -= OnAfterResponse;

            _isRunning = false;
            Log?.Invoke(this, "Proxy parado");
        }
        catch (Exception ex)
        {
            Log?.Invoke(this, $"Erro ao parar proxy: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _proxyServer.Dispose();
        GC.SuppressFinalize(this);
    }
}
