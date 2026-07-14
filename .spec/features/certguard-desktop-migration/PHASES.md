# Phases: certguard-desktop-migration

Gerado por /plan a partir de PLAN.md — view executável para `./ralph.sh .spec/features/certguard-desktop-migration/PHASES.md`.

## Phase 1: Solution Structure

Antes de implementar, leia:
1. `.spec/features/certguard-desktop-migration/SPEC.md` — requisitos RIGID que esta fase cobre
2. `.spec/features/certguard-desktop-migration/PLAN.md` — decomposição completa, dependências e riscos

- [ ] T01 — Create solution and project structure
      Arquivos: `CertGuard.sln`, `src/CertGuard.Core/CertGuard.Core.csproj`, `src/CertGuard.Services/CertGuard.Services.csproj`, `src/CertGuard.Desktop/CertGuard.Desktop.csproj`
      Mudança: Create the new multi-project solution with three projects: Core (classlib, net8.0), Services (classlib, net8.0), Desktop (wpf, net8.0-windows). Add inter-project references: Services → Core, Desktop → Services + Core.
      Cobre: RF-01
      Acceptance criteria: `dotnet build` from solution root completes with zero errors and produces CertGuardDesktop.exe output.
      Testes: `dotnet build` — zero errors, three project outputs produced.
- [ ] T02 — Install NuGet packages
      Arquivos: `src/CertGuard.Core/CertGuard.Core.csproj`, `src/CertGuard.Services/CertGuard.Services.csproj`, `src/CertGuard.Desktop/CertGuard.Desktop.csproj`
      Mudança: Add NuGet packages per project: Core → System.Security.Cryptography.ProtectedData; Services → Microsoft.Extensions.Http, Microsoft.Extensions.Hosting, System.Text.Json, System.Security.Cryptography.X509Certificates; Desktop → CommunityToolkit.Mvvm, CommunityToolkit.Mvvm.ComponentModel, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting, Hardcodet.Wpf.TaskbarNotification, Serilog, Serilog.Sinks.File, Titanium.Web.Proxy.
      Cobre: RF-01
      Acceptance criteria: `dotnet restore` completes without errors; `dotnet build` succeeds with all packages resolved.
      Testes: `dotnet restore` — no errors; `dotnet build` — success.

## Phase 2: Core Models & Interfaces

Antes de implementar, leia:
1. `.spec/features/certguard-desktop-migration/SPEC.md` — requisitos RIGID que esta fase cobre
2. `.spec/features/certguard-desktop-migration/PLAN.md` — decomposição completa, dependências e riscos

- [ ] T03 — Create Core models (User, Certificado, Device, Sessao)
      Arquivos: `src/CertGuard.Core/Models/User.cs`, `src/CertGuard.Core/Models/Certificado.cs`, `src/CertGuard.Core/Models/Device.cs`, `src/CertGuard.Core/Models/Sessao.cs`
      Mudança: Define POCO models matching the Laravel backend response shapes. Use nullable reference types, file-scoped namespaces, PascalCase properties.
      Cobre: RF-03, RF-09, RF-10
      Acceptance criteria: `dotnet build` compiles Core project with zero errors; all four model classes exist with correct properties matching API contract response shapes.
      Testes: `dotnet build src/CertGuard.Core` — zero errors.
- [ ] T04 — Create Core DTOs (LoginRequest, RegisterDeviceRequest, ActivateSessionRequest, etc.)
      Arquivos: `src/CertGuard.Core/DTOs/LoginRequest.cs`, `src/CertGuard.Core/DTOs/RegisterDeviceRequest.cs`, `src/CertGuard.Core/DTOs/ActivateSessionRequest.cs`, `src/CertGuard.Core/DTOs/HeartbeatResponse.cs`, `src/CertGuard.Core/DTOs/NavigationPolicyResponse.cs`, `src/CertGuard.Core/DTOs/LoginResponse.cs`
      Mudança: Define C# records for request/response DTOs matching the API contracts (CT-01 through CT-12). Each record maps 1:1 to the JSON contract.
      Cobre: RF-02, RF-03, RF-04, RF-05, RF-12, CT-01 through CT-12
      Acceptance criteria: `dotnet build` compiles Core project with zero errors; each DTO record has properties matching the corresponding API contract field names and types.
      Testes: `dotnet build src/CertGuard.Core` — zero errors.
- [ ] T05 — Create Core interfaces (IAuthService, ICertificateService, ISessionService, IDeviceService, ICertificateStoreService, IKeyGenService)
      Arquivos: `src/CertGuard.Core/Interfaces/IAuthService.cs`, `src/CertGuard.Core/Interfaces/ICertificateService.cs`, `src/CertGuard.Core/Interfaces/ISessionService.cs`, `src/CertGuard.Core/Interfaces/IDeviceService.cs`, `src/CertGuard.Core/Interfaces/ICertificateStoreService.cs`, `src/CertGuard.Core/Interfaces/IKeyGenService.cs`, `src/CertGuard.Core/Interfaces/IAuditService.cs`, `src/CertGuard.Core/Interfaces/IDomainPolicyService.cs`, `src/CertGuard.Core/Interfaces/ITokenStorage.cs`
      Mudança: Define service interfaces with async method signatures matching the RF requirements. Include ITokenStorage for DPAPI persistence and IAuditService for Event Log + backend sync.
      Cobre: RF-01, RF-02, RF-03, RF-04, RF-05, RF-06, RF-07, RF-09, RF-10, RF-11, RF-12
      Acceptance criteria: `dotnet build` compiles Core project with zero errors; all nine interfaces exist with correct method signatures.
      Testes: `dotnet build src/CertGuard.Core` — zero errors.
- [ ] T06 — Create Core models and domain types (DomainPolicy, NavigationPolicy)
      Arquivos: `src/CertGuard.Core/Models/DomainPolicy.cs`, `src/CertGuard.Core/Models/NavigationPolicy.cs`
      Mudança: Define models for domain policy lists (cert_usage_domains, validation_domains, blocked_domains) and navigation policy response. Include wildcard matching helper method.
      Cobre: RF-06, RF-12
      Acceptance criteria: `dotnet build` compiles Core project with zero errors; DomainPolicy and NavigationPolicy classes exist with string array properties for domain lists.
      Testes: `dotnet build src/CertGuard.Core` — zero errors.

## Phase 3: Services Layer

Antes de implementar, leia:
1. `.spec/features/certguard-desktop-migration/SPEC.md` — requisitos RIGID que esta fase cobre
2. `.spec/features/certguard-desktop-migration/PLAN.md` — decomposição completa, dependências e riscos

- [ ] T07 — Implement TokenStorage (DPAPI persistence)
      Arquivos: `src/CertGuard.Services/Auth/TokenStorage.cs`
      Mudança: Implement ITokenStorage using System.Security.Cryptography.ProtectedData.ProtectedData.Protect with DataProtectionScope.CurrentUser and entropy "CertGuard-v1". Methods: SaveTokenAsync(string token), GetTokenAsync(), ClearTokenAsync().
      Cobre: RF-02
      Acceptance criteria: TokenStorage implements ITokenStorage; SaveTokenAsync stores token via DPAPI; GetTokenAsync retrieves it; ClearTokenAsync removes it. Token persists across application restarts on the same user profile.
      Testes: Manual test — save token → close app → reopen → token retrievable from DPAPI storage.
- [ ] T18 — Implement AuthHandler (DelegatingHandler for Bearer token)
      Arquivos: `src/CertGuard.Services/Http/AuthHandler.cs`
      Mudança: Implement DelegatingHandler that reads token from ITokenStorage and attaches "Authorization: Bearer {token}" header to all outgoing requests. Skip login endpoint.
      Cobre: RF-02
      Acceptance criteria: AuthHandler inherits DelegatingHandler; outgoing HTTP requests include Bearer token header when token exists; login endpoint requests are not modified.
      Testes: Unit test — mock ITokenStorage returns token, verify outgoing request has Authorization header.
- [ ] T08 — Implement AuthService (login, logout, me)
      Arquivos: `src/CertGuard.Services/Auth/AuthService.cs`
      Mudança: Implement IAuthService using HttpClient. LoginAsync posts to /api/desktop/login, stores token via TokenStorage. LogoutAsync posts to /api/desktop/logout. GetMeAsync fetches /api/desktop/me. Use System.Text.Json for deserialization.
      Cobre: RF-02, CT-01, CT-02, CT-03
      Acceptance criteria: AuthService implements IAuthService; LoginAsync sends POST with email/password, receives token and user, saves token to DPAPI; LogoutAsync sends POST; GetMeAsync sends GET and returns User.
      Testes: Mock HTTP test — login returns token, token persisted to DPAPI; logout succeeds; getMe returns user data.
- [ ] T11 — Implement KeyGenService (RSA key pair generation)
      Arquivos: `src/CertGuard.Services/Crypto/KeyGenService.cs`
      Mudança: Implement IKeyGenService. GenerateKeyPairAsync creates RSA 2048-bit key pair via RSA.Create(2048), exports public key as PEM, encrypts private key with ProtectedData (DPAPI, CurrentUser, entropy "CertGuard-v1"). GetPublicKeyFingerprintAsync returns SHA-256 fingerprint.
      Cobre: RF-11
      Acceptance criteria: GenerateKeyPairAsync returns non-empty public key in PEM format and non-empty encrypted private key blob; public key is extractable without DPAPI decryption; private key is only recoverable via ProtectedData.Unprotect on the same user profile.
      Testes: Unit test — generated public key is non-empty PEM string; private key is non-empty byte array; DPAPI round-trip works on same user profile.
- [ ] T09 — Implement DeviceService (device registration)
      Arquivos: `src/CertGuard.Services/Devices/DeviceService.cs`
      Mudança: Implement IDeviceService. RegisterAsync generates device fingerprint (SHA-256 of hostname + MAC address), generates RSA key pair via KeyGenService, POSTs to /api/desktop/devices. Idempotent: caches device_id in memory after first registration.
      Cobre: RF-09, RF-11, CT-04, CT-05, CT-06
      Acceptance criteria: RegisterAsync sends POST with hostname, ip_address, so, fingerprint, public_key; returns device_id; subsequent calls reuse cached device_id without creating duplicate.
      Testes: Manual test — first call registers device, second call returns same device_id.
- [ ] T10 — Implement CertificateService (list, activate, deactivate)
      Arquivos: `src/CertGuard.Services/Certificates/CertificateService.cs`
      Mudança: Implement ICertificateService. ListAsync fetches /api/desktop/certificados. ActivateAsync calls SessionService.ActivateAsync, then CertificateStoreService.InstallPfxAsync. DeactivateAsync calls CertificateStoreService.RemoveByThumbprintAsync, then SessionService.DeactivateAsync.
      Cobre: RF-03, RF-10, CT-07, CT-08, CT-10
      Acceptance criteria: ListAsync returns list of Certificado from API; ActivateAsync installs cert to X509Store and returns session; DeactivateAsync removes cert from store and ends session.
      Testes: Integration test — activate installs cert to store, deactivate removes it.
- [ ] T14 — Implement CertificateStoreService (X509Store operations)
      Arquivos: `src/CertGuard.Services/Crypto/CertificateStoreService.cs`
      Mudança: Implement ICertificateStoreService. InstallPfxAsync loads PFX with X509KeyStorageFlags.PersistKeySet | Exportable, adds to X509Store(StoreName.My, StoreLocation.CurrentUser). RemoveByThumbprintAsync finds by thumbprint and removes. ExistsAsync checks existence. CleanupOrphansAsync removes non-system certificates.
      Cobre: RF-10, RNF-01
      Acceptance criteria: InstallPfxAsync returns thumbprint of installed cert; cert exists in X509Store after install; RemoveByThumbprintAsync removes cert by thumbprint; cert no longer exists after removal; private keys never written to disk outside Windows cert store.
      Testes: Manual test — install PFX → cert exists in store → remove → cert gone.
- [ ] T12 — Implement SessionService (activate, heartbeat, deactivate)
      Arquivos: `src/CertGuard.Services/Sessions/SessionService.cs`
      Mudança: Implement ISessionService. ActivateAsync POSTs to /api/desktop/sessoes with certificado_id, device_id, justification. HeartbeatAsync POSTs to /api/desktop/heartbeat with session_id. DeactivateAsync DELETEs /api/desktop/sessoes/{session_id}.
      Cobre: RF-03, RF-04, CT-08, CT-09, CT-10
      Acceptance criteria: ActivateAsync sends POST and returns Sessao with pfx_base64, pfx_password, session_id, expires_at; HeartbeatAsync sends POST and returns HeartbeatResponse with status; DeactivateAsync sends DELETE.
      Testes: Mock test — activate returns session with PFX data; heartbeat returns active/expired status; deactivate succeeds.
- [ ] T13 — Implement HeartbeatService (BackgroundService)
      Arquivos: `src/CertGuard.Services/Sessions/HeartbeatService.cs`
      Mudança: Implement HeartbeatService as Microsoft.Extensions.Hosting.BackgroundService. Sends POST /heartbeat every 30 ± 2 seconds. On expired/revoked response, removes cert from store via CertificateStoreService and deactivates session. SetSession method configures active session_id and thumbprint.
      Cobre: RF-04, RNF-02
      Acceptance criteria: HeartbeatService inherits BackgroundService; heartbeat fires within 30 ± 2 seconds; when backend returns status "expired" or "revoked", certificate is removed from store and session deactivated within 5 seconds.
      Testes: Manual test — heartbeat fires within 30s; expired response triggers cleanup.
- [ ] T16 — Implement DomainPolicyService (wildcard matching)
      Arquivos: `src/CertGuard.Services/Proxy/DomainPolicyService.cs`
      Mudança: Implement IDomainPolicyService. LoadPolicy from NavigationPolicyResponse. Three ConcurrentDictionary lists: cert_usage, validation, blocked. Wildcard matching: *.domain.tld matches subdomains but not bare domain. Methods: IsAllowedForInterception, CanUseCertificateA1, IsValidationDomain, IsBlocked.
      Cobre: RF-06, RF-12
      Acceptance criteria: Given cert-usage rule "*.receita.fazenda.gov.br", requests to www.receita.fazenda.gov.br and api.receita.fazenda.gov.br are intercepted; receita.fazenda.gov.br (exact, no subdomain) is not intercepted; evil.receita.fazenda.gov.br is intercepted. Case-insensitive matching.
      Testes: Unit test — wildcard matching for cert-usage, validation, and blocked domains; case-insensitive; exact domain not matched by wildcard.
- [ ] T17 — Implement AuditService (Event Log + backend sync)
      Arquivos: `src/CertGuard.Services/Audit/AuditService.cs`
      Mudança: Implement IAuditService. LogBlocked/LogCertificateInjection/LogNavigationViolation methods write to Windows Event Log (source "CertGuard") via EventLog.WriteEntry and POST to /api/desktop/navigation/events with event_type, process_name, target_domain, action_taken, detection_layer fields.
      Cobre: RF-07, CT-12
      Acceptance criteria: When a domain is blocked, an event appears in Windows Event Log with source "CertGuard" and the blocked domain; within 10 seconds, a corresponding record is created via POST /api/desktop/navigation/events.
      Testes: Manual test — blocked event appears in Event Log; POST to backend succeeds within 10s.
- [ ] T15 — Implement ProxyService (Titanium.Web.Proxy MITM)
      Arquivos: `src/CertGuard.Services/Proxy/ProxyService.cs`
      Mudança: Implement ProxyService using Titanium.Web.Proxy. Configure ProxyServer with ExplicitProxyEndPoint on configurable port (default 8888). Register BeforeTunnelConnectRequest (decrypt SSL for domains in cert-usage/validation lists), BeforeRequest (block domains, return block page), ClientCertificateSelectionCallback (inject client cert for cert-usage domains). Use DomainPolicyService for decisions.
      Cobre: RF-05, RF-06, RNF-05
      Acceptance criteria: Proxy starts on configurable port (default 8888); blocked domain returns block page HTML with HTTP 200; cert-usage domain gets client certificate in TLS handshake; non-intercepted domain passes as raw tunnel. Port is NOT hardcoded — read from ProxyService.Port property.
      Testes: Manual test — proxy starts on port 8888; blocked domain returns block page; cert-usage domain gets client cert; non-intercepted domain passes through.
- [ ] T24 — Implement navigation policy fetch on activation
      Arquivos: `src/CertGuard.Services/Proxy/NavigationPolicyService.cs`
      Mudança: After session activation, fetch GET /api/desktop/navigation/policy?session_id={session_id} and load response into DomainPolicyService. Cache policy in memory for session lifetime.
      Cobre: RF-12, CT-11
      Acceptance criteria: After session activation, DomainPolicyService.IsAllowedForInterception("*.receita.fazenda.gov.br") returns true if the backend returned that pattern in cert_usage_domains.
      Testes: Manual test — after activation, domain lists are populated from backend response.

## Phase 4: WPF Desktop

Antes de implementar, leia:
1. `.spec/features/certguard-desktop-migration/SPEC.md` — requisitos RIGID que esta fase cobre
2. `.spec/features/certguard-desktop-migration/PLAN.md` — decomposição completa, dependências e riscos

- [ ] T20 — Create ViewModels (AuthViewModel, CertificatesViewModel, SessionViewModel)
      Arquivos: `src/CertGuard.Desktop/ViewModels/AuthViewModel.cs`, `src/CertGuard.Desktop/ViewModels/CertificatesViewModel.cs`, `src/CertGuard.Desktop/ViewModels/SessionViewModel.cs`
      Mudança: Implement ViewModels using CommunityToolkit.Mvvm ObservableObject with [ObservableProperty] and [RelayCommand] source generators. AuthViewModel: login command, error handling. CertificatesViewModel: certificate list, activate/deactivate commands. SessionViewModel: countdown timer, expiry warning.
      Cobre: RF-01, RF-02, RF-03, RF-13
      Acceptance criteria: ViewModels compile with source generators; AuthViewModel has LoginCommand that calls AuthService.LoginAsync; CertificatesViewModel has ActivateCommand and DeactivateCommand; SessionViewModel has countdown properties.
      Testes: `dotnet build src/CertGuard.Desktop` — zero errors; source generators produce observable properties.
- [ ] T19 — Create WPF Views (LoginWindow, MainWindow, CertificatesPage, ExpiryDialog)
      Arquivos: `src/CertGuard.Desktop/Views/LoginWindow.xaml`, `src/CertGuard.Desktop/Views/LoginWindow.xaml.cs`, `src/CertGuard.Desktop/Views/MainWindow.xaml`, `src/CertGuard.Desktop/Views/MainWindow.xaml.cs`, `src/CertGuard.Desktop/Views/CertificatesPage.xaml`, `src/CertGuard.Desktop/Views/CertificatesPage.xaml.cs`, `src/CertGuard.Desktop/Views/ExpiryDialog.xaml`, `src/CertGuard.Desktop/Views/ExpiryDialog.xaml.cs`
      Mudança: Create WPF XAML views with data bindings to ViewModels. LoginWindow: email/password fields, error display, login button. MainWindow: navigation frame, tray integration. CertificatesPage: certificate list with activate/deactivate buttons. ExpiryDialog: countdown display, extend/close buttons. All strings in pt-BR.
      Cobre: RF-02, RF-03, RF-08, RF-13, RNF-03
      Acceptance criteria: UI renders with correct bindings; all user-facing strings in pt-BR; LoginWindow has email/password fields; CertificatesPage shows certificate list; ExpiryDialog shows countdown.
      Testes: `dotnet build src/CertGuard.Desktop` — zero errors; manual test — UI renders correctly.
- [ ] T22 — Implement TrayService (system tray integration)
      Arquivos: `src/CertGuard.Desktop/Services/TrayService.cs`
      Mudança: Implement tray integration using Hardcodet.Wpf.TaskbarNotification. MainWindow minimizes to tray on close button. Double-click tray icon restores window. Context menu: "Abrir" (restore), "Sair" (exit).
      Cobre: RF-08
      Acceptance criteria: Minimizing the window hides it from the taskbar and shows a tray icon; double-clicking the tray icon restores the window; right-clicking shows "Abrir" and "Sair" options; "Sair" terminates the application.
      Testes: Manual test — minimize hides from taskbar; tray icon appears; double-click restores; right-click shows menu; "Sair" exits.
- [ ] T23 — Implement expiry warning dialog (DispatcherTimer countdown)
      Arquivos: `src/CertGuard.Desktop/ViewModels/SessionViewModel.cs` (extend), `src/CertGuard.Desktop/Views/ExpiryDialog.xaml`
      Mudança: When session remaining time drops below 300 seconds (5 minutes), show ExpiryDialog with countdown updating every 1 second via DispatcherTimer. "Estender" re-activates session. "Encerrar" deactivates and removes certificate.
      Cobre: RF-13
      Acceptance criteria: With a session TTL of 60 minutes, the expiry dialog appears at 55 minutes remaining; the countdown decrements every second; clicking "Encerrar" deactivates the session and removes the certificate.
      Testes: Manual test — dialog appears at 5 minutes remaining; countdown decrements; "Encerrar" deactivates session.
- [ ] T21 — Implement DI setup (App.xaml.cs)
      Arquivos: `src/CertGuard.Desktop/App.xaml.cs`, `src/CertGuard.Desktop/App.xaml`
      Mudança: Configure Microsoft.Extensions.DependencyInjection in App.xaml.cs. Register all services (IAuthService, IDeviceService, ICertificateService, ISessionService, ICertificateStoreService, IKeyGenService, IAuditService, IDomainPolicyService, ITokenStorage) with appropriate lifetimes. Register HttpClientFactory with "backend" named client (base address https://homolog.lidderaplus.com.br/api). Register AuthHandler as DelegatingHandler. Register HeartbeatService as hosted service. Register Views as singletons.
      Cobre: RF-01
      Acceptance criteria: `dotnet build` compiles Desktop project; `dotnet run` launches application with DI container resolved; all services are resolvable from the DI container.
      Testes: `dotnet build src/CertGuard.Desktop` — zero errors; `dotnet run` — application launches.

## Phase 5: Proxy Integration & Audit Verification

Antes de implementar, leia:
1. `.spec/features/certguard-desktop-migration/SPEC.md` — requisitos RIGID que esta fase cobre
2. `.spec/features/certguard-desktop-migration/PLAN.md` — decomposição completa, dependências e riscos

> T15 (ProxyService) e T17 (AuditService) foram implementados na Fase 3. Esta fase integra o proxy ao Desktop via DI e verifica o fluxo completo de auditoria.

- [ ] T26 — Integrate ProxyService and AuditService into DI container and verify end-to-end flow
      Arquivos: `src/CertGuard.Desktop/App.xaml.cs`, `src/CertGuard.Services/Proxy/ProxyService.cs`
      Mudança: Ensure ProxyService and AuditService are registered in the DI container (App.xaml.cs). Wire ProxyService to use DomainPolicyService and AuditService. Verify the complete flow: proxy intercepts → domain policy check → blocked page or cert injection → audit event written to Event Log + backend.
      Cobre: RF-05, RF-06, RF-07
      Acceptance criteria: ProxyService is resolvable from DI; when proxy is running, blocked domain triggers AuditService.LogBlocked; Event Log entry appears with source "CertGuard"; POST /api/desktop/navigation/events is sent within 10s.
      Testes: Manual test — start proxy, access blocked domain, verify Event Log entry and backend sync.

## Phase 6: Background Services & Finalization

Antes de implementar, leia:
1. `.spec/features/certguard-desktop-migration/SPEC.md` — requisitos RIGID que esta fase cobre
2. `.spec/features/certguard-desktop-migration/PLAN.md` — decomposição completa, dependências e riscos

- [ ] T25 — Update CI workflow for multi-project solution
      Arquivos: `.github/workflows/build.yml`
      Mudança: Update CI to build the new CertGuard.sln instead of CertGuardMini.sln. Update publish command to output CertGuardDesktop.exe.
      Cobre: RF-01
      Acceptance criteria: CI workflow runs successfully on manual trigger; builds CertGuard.sln; publishes CertGuardDesktop.exe as artifact.
      Testes: CI workflow manual trigger — success; artifact uploaded with correct name.
