# Guia de Implementação: CertGuard Desktop (.NET 8 WPF)

## Pré-requisitos

1. .NET 8.0 SDK
2. Visual Studio 2022 (Community)
3. Git

---

## Fase 1: Estrutura Base

### 1.1 Criar solução

```bash
dotnet new sln -n CertGuard
```

### 1.2 Criar projetos

```bash
# Core (Models, DTOs, Interfaces)
dotnet new classlib -n CertGuard.Core -f net8.0
dotnet sln add CertGuard.Core

# Services (Business Logic)
dotnet new classlib -n CertGuard.Services -f net8.0
dotnet sln add CertGuard.Services

# Desktop (WPF App)
dotnet new wpf -n CertGuard.Desktop -f net8.0
dotnet sln add CertGuard.Desktop
```

### 1.3 Adicionar referências

```bash
cd CertGuard.Services
dotnet add reference ../CertGuard.Core

cd ../CertGuard.Desktop
dotnet add reference ../CertGuard.Services
dotnet add reference ../CertGuard.Core
```

### 1.4 Instalar NuGet packages

```bash
# CertGuard.Core
cd CertGuard.Core
dotnet add package System.Security.Cryptography.ProtectedData

# CertGuard.Services
cd ../CertGuard.Services
dotnet add package Microsoft.Extensions.Http
dotnet add package Microsoft.Extensions.Hosting
dotnet add package System.Text.Json
dotnet add package System.Security.Cryptography.X509Certificates

# CertGuard.Desktop
cd ../CertGuard.Desktop
dotnet add package CommunityToolkit.Mvvm
dotnet add package CommunityToolkit.Mvvm.ComponentModel
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Hardcodet.Wpf.TaskbarNotification
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Titanium.Web.Proxy
```

---

## Fase 2: Core (Models e Interfaces)

### 2.1 Models

```csharp
// CertGuard.Core/Models/User.cs
namespace CertGuard.Core.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// CertGuard.Core/Models/Certificado.cs
public class Certificado
{
    public int Id { get; set; }
    public string Apelido { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Cnpj { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DataVencimento { get; set; }
    public bool RequiresJustification { get; set; }
    public int SessionTtlMinutes { get; set; }
    public string[] AllowedWeekdays { get; set; } = [];
    public string? AllowedTimeStart { get; set; }
    public string? AllowedTimeEnd { get; set; }
}

// CertGuard.Core/Models/Device.cs
public class Device
{
    public int Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? So { get; set; }
    public string? Fingerprint { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

// CertGuard.Core/Models/Sessao.cs
public class Sessao
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionCode { get; set; } = string.Empty;
    public int CertificadoId { get; set; }
    public string? Cnpj { get; set; }
    public string? CommonName { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? PfxBase64 { get; set; }
    public string? PfxPassword { get; set; }
}
```

### 2.2 DTOs

```csharp
// CertGuard.Core/DTOs/LoginRequest.cs
namespace CertGuard.Core.DTOs;

public record LoginRequest(string Email, string Password);

// CertGuard.Core/DTOs/RegisterDeviceRequest.cs
public record RegisterDeviceRequest(
    string Hostname,
    string IpAddress,
    string So,
    string Fingerprint,
    string PublicKey);

// CertGuard.Core/DTOs/ActivateSessionRequest.cs
public record ActivateSessionRequest(
    int CertificadoId,
    int DeviceId,
    string? Justification);
```

### 2.3 Interfaces

```csharp
// CertGuard.Core/Interfaces/IAuthService.cs
namespace CertGuard.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<User> GetMeAsync();
}

// CertGuard.Core/Interfaces/IDeviceService.cs
public interface IDeviceService
{
    Task<Device> RegisterAsync(RegisterDeviceRequest request);
    Task<List<Device>> ListAsync();
    Task DeactivateAsync(int deviceId);
}

// CertGuard.Core/Interfaces/ICertificateService.cs
public interface ICertificateService
{
    Task<List<Certificado>> ListAsync();
}

// CertGuard.Core/Interfaces/ISessionService.cs
public interface ISessionService
{
    Task<Sessao> ActivateAsync(ActivateSessionRequest request);
    Task<HeartbeatResponse> HeartbeatAsync(string sessionId);
    Task DeactivateAsync(string sessionId);
}

// CertGuard.Core/Interfaces/ICertificateStoreService.cs
public interface ICertificateStoreService
{
    Task<string> InstallPfxAsync(byte[] pfxBytes, string password);
    Task RemoveByThumbprintAsync(string thumbprint);
    Task<bool> ExistsAsync(string thumbprint);
    Task CleanupOrphansAsync();
}

// CertGuard.Core/Interfaces/IKeyGenService.cs
public interface IKeyGenService
{
    Task<(string PublicKey, string PrivateKey)> GenerateKeyPairAsync();
    Task<string> GetPublicKeyFingerprintAsync();
}
```

---

## Fase 3: Services

### 3.1 AuthService

```csharp
// CertGuard.Services/Auth/AuthService.cs
using System.Net.Http.Json;
using CertGuard.Core.DTOs;
using CertGuard.Core.Interfaces;
using CertGuard.Core.Models;

namespace CertGuard.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/desktop/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    public async Task LogoutAsync()
    {
        await _httpClient.PostAsync("/api/desktop/logout", null);
    }

    public async Task<User> GetMeAsync()
    {
        return (await _httpClient.GetFromJsonAsync<User>(
            "/api/desktop/me"))!;
    }
}
```

### 3.2 CertificateStoreService

```csharp
// CertGuard.Services/Crypto/CertificateStoreService.cs
using System.Security.Cryptography.X509Certificates;
using CertGuard.Core.Interfaces;

namespace CertGuard.Services.Crypto;

public class CertificateStoreService : ICertificateStoreService
{
    public async Task<string> InstallPfxAsync(byte[] pfxBytes, string password)
    {
        return await Task.Run(() =>
        {
            var cert = new X509Certificate2(
                pfxBytes, password,
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable);

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();

            return cert.Thumbprint;
        });
    }

    public async Task RemoveByThumbprintAsync(string thumbprint)
    {
        await Task.Run(() =>
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var found = store.Certificates.Find(
                X509FindType.FindByThumbprint, thumbprint, false);

            foreach (var cert in found)
                store.Remove(cert);

            store.Close();
        });
    }

    public async Task<bool> ExistsAsync(string thumbprint)
    {
        return await Task.Run(() =>
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var found = store.Certificates.Find(
                X509FindType.FindByThumbprint, thumbprint, false);

            store.Close();
            return found.Count > 0;
        });
    }

    public async Task CleanupOrphansAsync()
    {
        await Task.Run(() =>
        {
            var systemSubjects = new[] { "CN=Microsoft", "CN=DigiCert", "CN=GlobalSign" };

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var toRemove = store.Certificates
                .Where(c => c.HasPrivateKey &&
                    !systemSubjects.Any(s => c.Subject.Contains(s)))
                .ToList();

            foreach (var cert in toRemove)
                store.Remove(cert);

            store.Close();
        });
    }
}
```

### 3.3 HeartbeatService

```csharp
// CertGuard.Services/Sessions/HeartbeatService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CertGuard.Core.Interfaces;

namespace CertGuard.Services.Sessions;

public class HeartbeatService : BackgroundService
{
    private readonly ILogger<HeartbeatService> _logger;
    private readonly ISessionService _sessionService;
    private readonly ICertificateStoreService _certStore;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    private string? _sessionId;
    private string? _thumbprint;

    public HeartbeatService(
        ILogger<HeartbeatService> logger,
        ISessionService sessionService,
        ICertificateStoreService certStore)
    {
        _logger = logger;
        _sessionService = sessionService;
        _certStore = certStore;
    }

    public void SetSession(string sessionId, string thumbprint)
    {
        _sessionId = sessionId;
        _thumbprint = thumbprint;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_sessionId != null)
            {
                try
                {
                    var response = await _sessionService.HeartbeatAsync(_sessionId);

                    if (response.Status is "expired" or "revoked")
                    {
                        _logger.LogWarning("Session {Status}, cleaning up", response.Status);
                        await CleanupAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Heartbeat failed");
                }
            }

            await Task.Delay(_interval, ct);
        }
    }

    private async Task CleanupAsync()
    {
        if (_thumbprint != null)
            await _certStore.RemoveByThumbprintAsync(_thumbprint);

        if (_sessionId != null)
            await _sessionService.DeactivateAsync(_sessionId);

        _sessionId = null;
        _thumbprint = null;
    }
}
```

---

## Fase 4: Desktop (WPF)

### 4.1 Program.cs

```csharp
// CertGuard.Desktop/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CertGuard.Core.Interfaces;
using CertGuard.Services.Auth;
using CertGuard.Services.Crypto;
using CertGuard.Services.Sessions;

var builder = Host.CreateApplicationBuilder(args);

// Services
builder.Services.AddHttpClient("backend", client =>
{
    client.BaseAddress = new Uri("https://homolog.lidderaplus.com.br/api");
});

builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IDeviceService, DeviceService>();
builder.Services.AddSingleton<ICertificateService, CertificateService>();
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton<ICertificateStoreService, CertificateStoreService>();
builder.Services.AddSingleton<IKeyGenService, KeyGenService>();
builder.Services.AddHostedService<HeartbeatService>();

// WPF
builder.Services.AddSingleton<MainWindow>();

var host = builder.Build();
var mainWindow = host.Services.GetRequiredService<MainWindow>();
mainWindow.Show();

await host.RunAsync();
```

### 4.2 LoginViewModel

```csharp
// CertGuard.Desktop/ViewModels/LoginViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CertGuard.Core.Interfaces;

namespace CertGuard.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var response = await _authService.LoginAsync(Email, Password);
            // Navigate to certificates
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 4.3 LoginWindow.xaml

```xml
<!-- CertGuard.Desktop/Views/LoginWindow.xaml -->
<Window x:Class="CertGuard.Desktop.Views.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="CertGuard" Height="400" Width="300"
        WindowStartupLocation="CenterScreen"
        Background="#1a1a2e">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Logo -->
        <TextBlock Grid.Row="0" Text="🔒 CertGuard"
                   FontSize="24" Foreground="White"
                   HorizontalAlignment="Center" Margin="0,0,0,20"/>

        <!-- Email -->
        <TextBox Grid.Row="1" Text="{Binding Email}"
                 PlaceholderText="Email"
                 Margin="0,0,0,10"/>

        <!-- Password -->
        <PasswordBox Grid.Row="2" x:Name="PasswordBox"
                     Margin="0,0,0,10"/>

        <!-- Error -->
        <TextBlock Grid.Row="3" Text="{Binding ErrorMessage}"
                   Foreground="#ff6b6b" Margin="0,0,0,10"
                   TextWrapping="Wrap"/>

        <!-- Login Button -->
        <Button Grid.Row="4" Content="Entrar"
                Command="{Binding LoginCommand}"
                Background="#4ecdc4" Foreground="White"
                Height="40" FontWeight="Bold"/>
    </Grid>
</Window>
```

---

## Fase 5: Proxy

### 5.1 Instalar Titanium.Web.Proxy

```bash
cd CertGuard.Services
dotnet add package Titanium.Web.Proxy
```

### 5.2 ProxyService

```csharp
// CertGuard.Services/Proxy/ProxyService.cs
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace CertGuard.Services.Proxy;

public class ProxyService
{
    private readonly ProxyServer _proxy;
    private readonly DomainPolicyService _policy;
    private readonly AuditService _audit;

    public ProxyService(DomainPolicyService policy, AuditService audit)
    {
        _proxy = new ProxyServer("CertGuard Root CA", "CertGuard");
        _policy = policy;
        _audit = audit;

        _proxy.BeforeTunnelConnectRequest += OnBeforeTunnelConnect;
        _proxy.BeforeRequest += OnBeforeRequest;
    }

    public void Start()
    {
        var endpoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, enableSsl: true);
        _proxy.AddEndPoint(endpoint);
        _proxy.Start();
        _proxy.SetAsSystemProxy(endpoint, ProxyProtocolType.AllHttp);
    }

    public void Stop()
    {
        _proxy.Stop();
        _proxy.RestoreOriginalProxySettings();
    }

    private async Task OnBeforeTunnelConnect(object sender, TunnelConnectSessionEventArgs e)
    {
        string host = e.HttpClient.Request.RequestUri.Host;
        e.DecryptSsl = _policy.IsAllowedForInterception(host);
    }

    private async Task OnBeforeRequest(object sender, SessionEventArgs e)
    {
        string host = new Uri(e.HttpClient.Request.Url).Host;

        if (_policy.IsBlocked(host))
        {
            await _audit.LogBlocked(host, "BLOCKLIST");
            e.Ok("<html><body><h1>Acesso Bloqueado</h1></body></html>");
        }
    }
}
```

### 5.3 DomainPolicyService

```csharp
// CertGuard.Services/Proxy/DomainPolicyService.cs
using System.Collections.Concurrent;

namespace CertGuard.Services.Proxy;

public class DomainPolicyService
{
    private readonly ConcurrentDictionary<string, byte> _certUsage = new();
    private readonly ConcurrentDictionary<string, byte> _validation = new();
    private readonly ConcurrentDictionary<string, byte> _blocked = new();

    public void LoadPolicy(DomainPolicy policy)
    {
        _certUsage.Clear();
        foreach (var d in policy.CertUsageDomains) _certUsage[d] = 1;

        _validation.Clear();
        foreach (var d in policy.ValidationDomains) _validation[d] = 1;

        _blocked.Clear();
        foreach (var d in policy.BlockedDomains) _blocked[d] = 1;
    }

    public bool IsAllowedForInterception(string host) =>
        _certUsage.ContainsKey(host) || _validation.ContainsKey(host);

    public bool CanUseCertificateA1(string host) =>
        MatchDomain(host, _certUsage);

    public bool IsValidationDomain(string host) =>
        MatchDomain(host, _validation);

    public bool IsBlocked(string host) =>
        MatchDomain(host, _blocked);

    private bool MatchDomain(string host, ConcurrentDictionary<string, byte> list)
    {
        if (list.ContainsKey(host)) return true;

        foreach (var pattern in list.Keys)
        {
            if (pattern.StartsWith("*."))
            {
                string suffix = pattern[1..];
                if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }
}
```

---

## Ordem de Implementação

1. ✅ Estrutura base (solução + projetos)
2. ✅ Core (Models + DTOs + Interfaces)
3. ✅ Services (Auth, Crypto, Session)
4. ✅ Desktop (WPF Views + ViewModels)
5. ✅ Proxy (Titanium.Web.Proxy)
6. ✅ Audit (EventLog + Backend sync)
7. ✅ Testes E2E

---

*Documento: IMPLEMENTATION-GUIDE.md*
*Versão: 1.0*
*Data: 14/07/2026*
