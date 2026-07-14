# Bloqueio de Domínios e Controle de Assinadores

## Visão Geral

O CertGuard Desktop controla **onde** e **por quem** o certificado A1 pode ser usado, implementando 3 camadas de proteção.

---

## Camada 1: Bloqueio de Domínios (Proxy MITM)

### Como funciona

```
App (Chrome/Adobe/Java)
  │
  │  CONNECT host:443
  │
  ▼
Proxy (.NET) ──── Titanium.Web.Proxy
  │
  ├─ BeforeTunnelConnectRequest
  │   └─ IsAllowedForInterception(host)?
  │       ├─ SIM → DecryptSsl = true (MITM)
  │       └─ NÃO → DecryptSsl = false (tunnel puro)
  │
  ├─ ClientCertificateSelectionCallback
  │   └─ CanUseCertificateA1(host)?
  │       ├─ SIM → e.ClientCertificate = certA1
  │       └─ NÃO → null (sem cert client)
  │
  └─ BeforeRequest
      └─ IsBlocked(host)?
          ├─ SIM → e.Ok(blockedPage)
          └─ NÃO → passa tráfego
```

### Eventos do Proxy

| Evento | Quando dispara | O que faz |
|--------|---------------|-----------|
| `BeforeTunnelConnectRequest` | App faz CONNECT host:443 | Decide se intercepta (DecryptSsl) |
| `ClientCertificateSelectionCallback` | Proxy precisa de client cert | Injeta cert A1 para domínios OK |
| `BeforeRequest` | Request HTTP dentro do tunnel | Bloqueia ou permite |

### Listas de Domínios

```csharp
public static class DomainLists
{
    // Onde o cert A1 PODE ser enviado (mTLS)
    public static readonly string[] CertUsage = {
        "*.receita.fazenda.gov.br",
        "*.sefaz.rs.gov.br",
        "*.prefeitura.sp.gov.br",
        "sped.rfb.gov.br",
        "*.nfse.gov.br"
    };

    // OCSP/CRL — sempre liberado (validação)
    public static readonly string[] Validation = {
        "acraiz.icpbrasil.gov.br",
        "*.certisign.com.br",
        "*.entrust.net",
        "*.digicert.com",
        "ocsp.adobe.com"
    };

    // Timestamp — sempre liberado
    public static readonly string[] Timestamp = {
        "timestamp.digicert.com",
        "tsa.starfieldtech.com"
    };

    // Sempre bloqueado
    public static readonly string[] Blocked = { };
}
```

### Fluxo: App Acessa Site Bloqueado

```
1. App envia: CONNECT site-bloqueado.com:443
2. Proxy: IsAllowedForInterception("site-bloqueado.com") → FALSE
3. DecryptSsl = false (tunnel puro)
4. App não consegue acessar
5. AuditService.LogBlocked("site-bloqueado.com", "chrome.exe", "NOT_IN_ALLOWLIST")
6. EventLog.WriteEntry("BLOQUEADO: site-bloqueado.com")
7. POST /api/desktop/navigation-events
```

### Fluxo: Assinador (Adobe) Valida Cert

```
1. Adobe abre PDF assinado
2. Adobe consulta OCSP: GET ocsp.acraiz.icpbrasil.gov.br
3. Proxy: IsValidationDomain("ocsp.acraiz.icpbrasil.gov.br") → TRUE
4. DecryptSsl = false (tunnel puro, sem interceptação)
5. Conexão OCSP passa direto
6. Adobe valida cadeia normalmente
✅ Funciona sem bloqueio
```

---

## Camada 2: Controle por Processo

### ACL na Chave Privada

Restringir **quais contas de usuário** podem acessar a chave:

```csharp
public void RestrictPrivateKeyAccess(X509Certificate2 cert, string[] allowedUsers)
{
    var rsa = cert.GetRSAPrivateKey();
    string keyPath = GetKeyPath(rsa);

    var security = new FileInfo(keyPath).GetAccessControl();
    security.SetAccessRuleProtection(true, false);

    foreach (string user in allowedUsers)
    {
        var rule = new FileSystemAccessRule(
            user, FileSystemRights.Read, AccessControlType.Allow);
        security.AddAccessRule(rule);
    }

    new FileInfo(keyPath).SetAccessControl(security);
}
```

### ETW — Detectar Qual Processo Acessa

```csharp
public class CryptoMonitor
{
    public void Start(Action<int, string, string> onAccess)
    {
        var session = new TraceEventSession("CertGuard-Monitor");
        session.EnableKernelProvider(
            KernelTraceEventParser.Keywords.FileIOInit);

        session.Source.Kernel.FileIOFileCreateEnd += (data) =>
        {
            if (data.FileName.Contains("\\Crypto\\Keys\\"))
            {
                onAccess(data.ProcessID, data.ProcessName, data.FileName);
            }
        };

        Task.Run(() => session.Source.Process());
    }
}
```

### Detecção de Assinadores

| Assinador | Como detectar | Domínios que acessa |
|-----------|--------------|---------------------|
| Adobe Acrobat | Processo `Acrobat.exe` | OCSP/CRL (*.certisign.com.br) |
| Java (bancos) | Processo `java.exe` | Domínios do banco |
| Chrome/Edge | Processo `chrome.exe` | Via system proxy |
| Firefox | Processo `firefox.exe` | **NÃO usa system proxy** |
| SignTool | Processo `SignTool.exe` | Não acessa rede |

---

## Camada 3: Auditoria

### Windows Event Log

```csharp
EventLog.WriteEntry("CertGuard",
    $"BLOQUEADO: {hostname} | Processo: {process} | Motivo: {reason}",
    EventLogEntryType.Warning);
```

### Backend Laravel

```csharp
await _httpClient.PostAsync("/api/desktop/navigation-events", new {
    type = "blocked",
    hostname, process, reason,
    timestamp = DateTime.UtcNow
});
```

### Log Local JSON

```csharp
File.AppendAllText("logs/blocked.jsonl",
    JsonSerializer.Serialize(new {
        hostname, process, reason,
        time = DateTime.UtcNow
    }));
```

---

## Matriz de Cobertura

| Evento | Proxy | ACL | ETW | Event Log |
|--------|-------|-----|-----|-----------|
| App envia cert para domínio bloqueado | **BLOQUEIA** | - | - | ✅ |
| App acessa chave privada | - | **BLOQUEIA** | ✅ | ✅ |
| Processo não autorizado acessa cert | - | - | **DETECTA** | ✅ |
| Assinador valida cert (OCSP) | PERMITE | - | - | ✅ |
| Usuário remove cert do store | - | - | ✅ | ✅ |

---

*Documento: DOMAIN-BLOCKING.md*
*Versão: 1.0*
*Data: 14/07/2026*
