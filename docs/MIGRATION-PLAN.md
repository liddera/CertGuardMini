# Plano de Migração: CertGuard Desktop (Electron → .NET 8 WPF)

## Visão Geral

Migração do **certguard-desktop** (Electron + React + TypeScript) para **CertGuard Desktop** (.NET 8 WPF), aproveitando o protótipo funcional do CertGuardMini.

---

## Por que migrar?

| Aspecto | Electron (atual) | .NET WPF (novo) |
|---------|-----------------|-----------------|
| MITM Proxy | Biblioteca JS (imatura) | Titanium.Web.Proxy (maduro) |
| Injeção de client cert | Não suporta nativamente | `ClientCertificateSelectionCallback` |
| Remover cert do store | PowerShell via child_process | `X509Store.Remove()` nativo |
| Salvar chave privada | safeStorage (variável) | DPAPI nativo Windows |
| Detectar processo | Não detecta | ETW + WMI |
| Escrever Event Log | Não escreve | `EventLog.WriteEntry` |
| ACL na chave | Não faz | `FileSystemAccessControl` |
| Tamanho binário | ~180MB | ~30MB (-83%) |
| RAM | ~150MB | ~40MB (-73%) |
| Startup | ~3-5s | ~0.5s (-85%) |

---

## Funcionalidades a Migrar

### 1. Auth (Login)

| Electron | .NET |
|----------|------|
| LoginScreen.tsx | LoginWindow.xaml |
| auth.store.ts (Zustand) | AuthViewModel.cs (ObservableObject) |
| auth.service.ts | AuthService.cs (HttpClient) |
| apiClient.ts (Axios) | ApiClient.cs + AuthHandler.cs |

### 2. Certificados

| Electron | .NET |
|----------|------|
| CertList.tsx | CertificatesPage.xaml |
| CertCard.tsx | CertificateCardControl.xaml |
| JustificativaModal.tsx | JustificativaDialog.xaml |
| cert.service.ts | CertificateService.cs |
| device.service.ts | DeviceService.cs |

### 3. Sessões

| Electron | .NET |
|----------|------|
| SessaoManager.tsx | HeartbeatService.cs (BackgroundService) |
| ExpiryModal.tsx | ExpiryDialog.xaml |
| session.store.ts | SessionViewModel.cs |
| session.service.ts | SessionService.cs |
| useHeartbeat.ts | HeartbeatService.cs + DispatcherTimer |

### 4. Crypto/Store

| Electron | .NET |
|----------|------|
| keygenService.ts (node-forge) | KeyGenService.cs (RSA.Create) |
| powershellService.ts | CertificateStoreService.cs (X509Store nativo) |
| safeStorage (Electron) | ProtectedData (DPAPI) |

### 5. System

| Electron | .NET |
|----------|------|
| trayService.ts | TrayService.cs (Hardcodet) |
| logger.ts | SanitizedLogger.cs (Serilog) |
| systemHandlers.ts | SystemService.cs (Environment, WMI) |
| certHandlers.ts | Commands nos ViewModels |

---

## APIs Consumidas (idênticas)

| Método | Endpoint | Uso |
|--------|----------|-----|
| POST | `/api/desktop/login` | Login |
| POST | `/api/desktop/logout` | Logout |
| GET | `/api/desktop/me` | Dados do user |
| POST | `/api/desktop/devices` | Registrar device |
| GET | `/api/desktop/devices` | Listar devices |
| DELETE | `/api/desktop/devices/{id}` | Desativar device |
| GET | `/api/desktop/certificados` | Listar certs |
| POST | `/api/desktop/sessoes` | Ativar sessão |
| POST | `/api/desktop/heartbeat` | Heartbeat |
| DELETE | `/api/desktop/sessoes/{id}` | Encerrar sessão |

---

## Estrutura do Projeto .NET

```
CertGuard Desktop/
├── CertGuard.sln
├── src/
│   ├── CertGuard.Core/           # Models, DTOs, Interfaces
│   ├── CertGuard.Services/       # Business Logic
│   ├── CertGuard.Desktop/        # WPF App
│   └── CertGuard.Hooks/          # Detours (opcional)
└── tests/
```

---

## Dependências NuGet

| Pacote | Uso |
|--------|-----|
| CommunityToolkit.Mvvm | MVVM |
| System.Security.Cryptography.ProtectedData | DPAPI |
| Microsoft.Extensions.Http | HttpClientFactory |
| Microsoft.Extensions.Hosting | BackgroundService |
| Titanium.Web.Proxy | MITM Proxy |
| Hardcodet.Wpf.TaskbarNotification | System tray |
| Serilog | Logging |

---

## Fluxos de Dados

### Login
```
LoginViewModel → AuthService.LoginAsync() → POST /login
  → TokenStorage.SaveToken() → ProtectedData (DPAPI)
  → NavigateTo<CertificatesViewModel>()
```

### Ativação de Certificado
```
CertificateService.ActivateAsync()
  → SessionService.ActivateAsync() → POST /sessoes
  → CertificateStoreService.InstallPfxAsync() → X509Store.Add()
  → HeartbeatService.StartAsync()
```

### Heartbeat
```
HeartbeatService (BackgroundService, 30s)
  → SessionService.HeartbeatAsync() → POST /heartbeat
  → Se expired → CleanupAsync() → X509Store.Remove()
```

### Desativação
```
CertificateService.DeactivateAsync()
  → CertificateStoreService.RemoveAsync() → X509Store.Remove()
  → SessionService.DeactivateAsync() → DELETE /sessoes/{id}
  → HeartbeatService.StopAsync()
```

---

## Riscos da Migração

| Risco | Severidade | Mitigação |
|-------|-----------|-----------|
| TitleBar frameless | 🟠 Alto | WindowChrome + drag behavior |
| UI visual diferente | 🟡 Médio | ResourceDictionary + estilos |
| Testes X509Store | 🟡 Médio | Testar em Win 10/11 |
| Firefox não usa proxy | 🟡 Médio | Config manual ou GPO |

---

## Ordem de Implementação

1. **Fase 1**: Backend — Migration navigation_policies + navigation_rules
2. **Fase 2**: CertGuard.Core — Models, DTOs, Interfaces
3. **Fase 3**: CertGuard.Services — Auth, Certificate, Session, Crypto
4. **Fase 4**: CertGuard.Desktop — WPF Views + ViewModels
5. **Fase 5**: Proxy — Titanium.Web.Proxy + DomainPolicy
6. **Fase 6**: Audit — ETW + EventLog + Backend sync
7. **Fase 7**: Testes E2E

---

*Documento: MIGRATION-PLAN.md*
*Versão: 1.0*
*Data: 14/07/2026*
