using System.Net;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

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
            await Task.CompletedTask;
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

        Log?.Invoke(this, $">>> Requisição: {host}");

        if (!_broker.IsDomainAllowed(host))
        {
            e.Ok(Encoding.UTF8.GetBytes(GenerateBlockedPage(host)));
            RequestBlocked?.Invoke(this, host);
            Log?.Invoke(this, $"BLOQUEADO: {host}");
            return;
        }

        RequestAllowed?.Invoke(this, host);
        Log?.Invoke(this, $"PERMITIDO: {host}");
    }

    private Task OnAfterResponse(object sender, SessionEventArgs e)
    {
        return Task.CompletedTask;
    }

    private string GenerateBlockedPage(string domain)
    {
        return $@"<!DOCTYPE html>
<html>
<head><title>CertGuard Mini - Bloqueado</title>
<style>
body {{ font-family:'Segoe UI',sans-serif; background:#1a1a2e; color:#e0e0e0;
       display:flex; justify-content:center; align-items:center; height:100vh; margin:0; }}
.card {{ background:#16213e; border:2px solid #e94560; border-radius:12px;
        padding:40px; text-align:center; max-width:500px; }}
h1 {{ color:#e94560; }}
.domain {{ background:#0f3460; padding:10px; border-radius:6px;
           font-family:monospace; color:#e94560; margin:15px 0; }}
</style></head>
<body>
<div class='card'>
<h1>Acesso Bloqueado</h1>
<p>O domínio nao esta na lista de permitidos:</p>
<div class='domain'>{domain}</div>
<p>CertGuard Mini esta protegendo seu trafego.</p>
</div></body></html>";
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
