# Referência Rápida — CertGuard Desktop

## Resumo do Projeto

**CertGuard Desktop** é um app Windows que controla o uso de certificados digitais A1, bloqueando acesso a sites não autorizados e monitorando todos os acessos.

---

## Stack

| Camada | Tecnologia |
|--------|------------|
| UI | WPF + XAML |
| MVVM | CommunityToolkit.Mvvm |
| HTTP | HttpClient + Refit |
| Proxy | Titanium.Web.Proxy |
| Crypto | System.Security.Cryptography |
| Logging | Serilog + EventLog |
| Backend | Laravel (alvras) |

---

## Funcionalidades

### 1. Autenticação
- Login via API Laravel
- Token armazenado com DPAPI
- Auto-logout em 401

### 2. Gerenciamento de Certificados
- Lista certificados visíveis
- Ativação: instala PFX no Windows Store
- Desativação: remove do Store
- Heartbeat: monitora status

### 3. Bloqueio de Domínios
- Proxy MITM intercepta HTTPS
- Injeta cert A1 SÓ para domínios permitidos
- Bloqueia sites não autorizados
- Libera OCSP/CRL para validação

### 4. Controle por Processo
- ACL na chave privada
- ETW monitor detecta processos
- Detecção de assinadores (Adobe, Java, etc.)

### 5. Auditoria
- Windows Event Log
- Backend Laravel (sync)
- Log local JSON

---

## Endpoints da API

| Método | Endpoint | Uso |
|--------|----------|-----|
| POST | `/api/desktop/login` | Login |
| POST | `/api/desktop/logout` | Logout |
| GET | `/api/desktop/me` | Dados do user |
| POST | `/api/desktop/devices` | Registrar device |
| GET | `/api/desktop/certificados` | Listar certs |
| POST | `/api/desktop/sessoes` | Ativar sessão |
| POST | `/api/desktop/heartbeat` | Heartbeat |
| DELETE | `/api/desktop/sessoes/{id}` | Encerrar sessão |
| GET | `/api/desktop/navigation/policy` | Política de domínios |
| POST | `/api/desktop/navigation/events` | Registrar violação |

---

## Fluxos Principais

### Login → Ativação → Heartbeat → Desativação

```
1. POST /login → { token }
2. POST /devices → { device_id }
3. GET /certificados → { certificados[] }
4. POST /sessoes → { session_id, pfx_base64, expires_at }
5. POST /heartbeat (30s) → { status, expires_at }
6. DELETE /sessoes/{id} → { success }
```

### Bloqueio de Domínio

```
1. App envia CONNECT host:443
2. Proxy: IsAllowedForInterception(host)?
   ├─ SIM → DecryptSsl = true → ClientCertificateSelection
   │        └─ CanUseCertificateA1(host)?
   │            ├─ SIM → injeta cert A1
   │            └─ NÃO → sem cert
   └─ NÃO → DecryptSsl = false (tunnel puro)
3. BeforeRequest: IsBlocked(host)?
   ├─ SIM → e.Ok(blockedPage) + audit
   └─ NÃO → passa tráfego
```

---

## Estrutura de Pastas

```
CertGuard Desktop/
├── CertGuard.Core/           # Models, DTOs, Interfaces
├── CertGuard.Services/       # Business Logic
│   ├── Auth/
│   ├── Certificates/
│   ├── Sessions/
│   ├── Crypto/
│   ├── Proxy/
│   └── Audit/
├── CertGuard.Desktop/        # WPF App
│   ├── Views/
│   ├── ViewModels/
│   └── Resources/
└── tests/
```

---

## Comandos Úteis

```bash
# Compilar
dotnet build

# Rodar
dotnet run --project CertGuard.Desktop

# Publicar
dotnet publish -c Release -r win-x64 --self-contained

# Testar
dotnet test
```

---

## Documentação

- [MIGRATION-PLAN.md](MIGRATION-PLAN.md) — Plano de migração
- [ARCHITECTURE.md](ARCHITECTURE.md) — Arquitetura
- [API-ENDPOINTS.md](API-ENDPOINTS.md) — Endpoints
- [DOMAIN-BLOCKING.md](DOMAIN-BLOCKING.md) — Bloqueio
- [AUDIT-SYSTEM.md](AUDIT-SYSTEM.md) — Auditoria
- [COMPARISON.md](COMPARISON.md) — Electron vs .NET
- [IMPLEMENTATION-GUIDE.md](IMPLEMENTATION-GUIDE.md) — Guia de implementação

---

*Referência Rápida — CertGuard Desktop v1.0*
