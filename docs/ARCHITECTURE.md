# Arquitetura: CertGuard Desktop (.NET 8 WPF)

## Diagrama Geral

```
┌─────────────────────────────────────────────────────────────────┐
│                    CERTGUARD DESKTOP (.NET 8 WPF)                │
│                                                                  │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐        │
│  │  UI Layer    │   │  ViewModel   │   │  Services    │        │
│  │  (XAML)      │◄─►│  (MVVM)      │◄─►│  (Business)  │        │
│  └──────────────┘   └──────────────┘   └──────┬───────┘        │
│                                                │                 │
│                                    ┌───────────┴───────────┐    │
│                                    │                       │    │
│                              ┌─────▼─────┐          ┌──────▼──┐│
│                              │  Proxy    │          │ Crypto  ││
│                              │  Service  │          │ Service ││
│                              │ (Titanium)│          │(X509St) ││
│                              └─────┬─────┘          └──────┬──┘│
│                                    │                       │    │
└────────────────────────────────────┼───────────────────────┼────┘
                                     │                       │
                              ┌──────▼──────┐         ┌──────▼──────┐
                              │  Windows    │         │  Windows    │
                              │  Cert Store │         │  Proxy      │
                              │ (CurrentUser│         │ (WinINET)   │
                              └─────────────┘         └─────────────┘
                                     │
                              ┌──────▼──────┐
                              │  Backend    │
                              │  Laravel    │
                              │  (alvras)   │
                              └─────────────┘
```

---

## Camadas

### 1. UI Layer (XAML)

Responsável pela apresentação visual. Usa WPF Data Binding para comunicação com ViewModels.

**Componentes:**
- `LoginWindow.xaml` — Tela de login
- `MainWindow.xaml` — Janela principal com tray
- `CertificatesPage.xaml` — Lista de certificados
- `CertificateCardControl.xaml` — Card individual de certificado
- `JustificativaDialog.xaml` — Modal de justificativa
- `ExpiryDialog.xaml` — Modal de aviso de expiração
- `TitleBar.xaml` — Barra de título customizada

### 2. ViewModel Layer (MVVM)

Responsável pela lógica de apresentação. Usa CommunityToolkit.Mvvm para observable properties e commands.

**ViewModels:**
- `AuthViewModel.cs` — Login, logout, estado de autenticação
- `CertificatesViewModel.cs` — Lista de certs, filtros, ativação
- `CertificateCardViewModel.cs` — Estado individual do card
- `SessionViewModel.cs` — Countdown, expiry, status da sessão

### 3. Services Layer (Business Logic)

Responsável pela lógica de negócio e comunicação com APIs externas.

**Services:**
- `AuthService.cs` — Login/logout/me via API
- `CertificateService.cs` — CRUD de certificados
- `DeviceService.cs` — Registro de dispositivos
- `SessionService.cs` — Ativação/heartbeat/desativação
- `KeyGenService.cs` — Geração de chaves RSA
- `CertificateStoreService.cs` — Install/remove do Windows Store
- `DomainPolicyService.cs` — Regras de domínio
- `HeartbeatService.cs` — BackgroundService para heartbeat
- `AuditService.cs` — Logging e auditoria
- `TokenStorage.cs` — Persistência de token (DPAPI)

### 4. Infrastructure Layer

**Comunicação HTTP:**
- `ApiClient.cs` — HttpClient wrapper
- `AuthHandler.cs` — DelegatingHandler (Bearer token)

**Proxy:**
- `ProxyService.cs` — Titanium.Web.Proxy
- `DomainPolicy.cs` — Listas de domínios

**Crypto:**
- `CertificateStoreService.cs` — X509Store nativo
- `KeyGenService.cs` — RSA.Create + ProtectedData

**Monitoramento:**
- `CryptoAccessMonitor.cs` — ETW tracing
- `ProcessMonitor.cs` — WMI process enumeration

---

## Fluxo de Dados

### Ativação de Certificado

```
1. User clica "Ativar" no CertificateCard
2. CertificatesViewModel.ActivateCommand
3. DeviceService.EnsureRegisteredAsync()
   → KeyGenService.GetOrCreateKeyPairAsync() → RSA.Create(2048)
   → DeviceService.RegisterAsync() → POST /devices
4. SessionService.ActivateAsync()
   → POST /sessoes → { pfx_base64, pfx_password, session_id, expires_at }
5. CertificateStoreService.InstallPfxAsync()
   → new X509Certificate2(pfxBytes, password)
   → X509Store.Add(cert)
6. HeartbeatService.StartAsync(session)
7. SessionViewModel.UpdateFromSession(session)
```

### Heartbeat

```
HeartbeatService (BackgroundService, 30s)
  → SessionService.HeartbeatAsync(sessionId) → POST /heartbeat
  → Se active: SessionViewModel.UpdateExpiresAt(expiresAt)
  → Se expired: CleanupAsync()
    → CertificateStoreService.RemoveAsync(thumbprint) → X509Store.Remove()
    → SessionService.DeactivateAsync() → DELETE /sessoes/{id}
  → DispatcherTimer (1s): SessionViewModel.UpdateCountdown()
  → Se countdown <= 300s: SessionViewModel.ShowExpiryModal = true
```

### Bloqueio de Domínio

```
1. App envia CONNECT host:443
2. Proxy.BeforeTunnelConnectRequest
3. DomainPolicyService.IsAllowedForInterception(host) → true/false
4. Se true: DecryptSsl = true (MITM)
   → ClientCertificateSelectionCallback
   → CanUseCertificateA1(host)
   → Se true: e.ClientCertificate = certA1
5. Se false: DecryptSsl = false (tunnel puro)
6. Proxy.BeforeRequest
7. IsBlocked(host) → true: e.Ok(blockedPage)
8. AuditService.LogBlocked(host, process, reason)
```

---

## Segurança

### DPAPI (ProtectedData)

```csharp
// Salvar chave privada
byte[] encrypted = ProtectedData.Protect(
    privateKeyBytes,
    entropy: Encoding.UTF8.GetBytes("CertGuard-v1"),
    scope: DataProtectionScope.CurrentUser);

// Carregar chave privada
byte[] decrypted = ProtectedData.Unprotect(
    encrypted, entropy, DataProtectionScope.CurrentUser);
```

### Sanitização de Logs

```csharp
// Serilog enricher que redacta chaves sensíveis
public class SensitiveDataEnricher : ILogEventEnricher {
    private static readonly string[] SensitiveKeys = {
        "password", "pfx", "private_key", "token", "authorization"
    };
}
```

### Windows Event Log

```csharp
// Escrever evento de auditoria
EventLog.WriteEntry("CertGuard",
    $"BLOQUEADO: {hostname} | Processo: {process}",
    EventLogEntryType.Warning);
```

---

## Diagrama de Componentes

```
CertGuard Desktop
├── Auth
│   ├── LoginWindow.xaml
│   ├── LoginViewModel.cs
│   ├── AuthService.cs
│   └── TokenStorage.cs
├── Certificates
│   ├── CertificatesPage.xaml
│   ├── CertificatesViewModel.cs
│   ├── CertificateCardControl.xaml
│   ├── CertificateCardViewModel.cs
│   ├── CertificateService.cs
│   ├── DeviceService.cs
│   └── CertificateStoreService.cs
├── Sessions
│   ├── SessionViewModel.cs
│   ├── HeartbeatService.cs
│   ├── SessionService.cs
│   ├── ExpiryDialog.xaml
│   └── JustificativaDialog.xaml
├── Proxy
│   ├── ProxyService.cs
│   ├── DomainPolicy.cs
│   └── DomainPolicyService.cs
├── Audit
│   ├── AuditService.cs
│   ├── CryptoAccessMonitor.cs
│   └── ProcessMonitor.cs
├── Crypto
│   ├── KeyGenService.cs
│   └── CertificateStoreService.cs
└── System
    ├── TrayService.cs
    ├── SystemService.cs
    └── NavigationService.cs
```

---

*Documento: ARCHITECTURE.md*
*Versão: 1.0*
*Data: 14/07/2026*
