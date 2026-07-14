# Sistema de Auditoria e Monitoramento

## Visão Geral

O CertGuard Desktop implementa 5 camadas de monitoramento para rastrear todos os acessos ao certificado A1.

---

## Camadas de Monitoramento

```
┌─────────────────────────────────────────────────────────┐
│  CAMADA 1: MITM Proxy (Bloqueio Ativo)                  │
│  → Intercepta HTTPS, bloqueia domínios não autorizados   │
├─────────────────────────────────────────────────────────┤
│  CAMADA 2: API Hooking (Detecção em Tempo Real)         │
│  → Intercepta chamadas de criptografia no processo       │
├─────────────────────────────────────────────────────────┤
│  CAMADA 3: SACL (Auditoria Passiva)                     │
│  → Windows gera eventos quando alguém acessa a chave     │
├─────────────────────────────────────────────────────────┤
│  CAMADA 4: Sysmon (Monitoramento de Processos)          │
│  → Detecta criação de processos e acesso a arquivos      │
├─────────────────────────────────────────────────────────┤
│  CAMADA 5: CNG ETW (Forense)                            │
│  → Log de alto desempenho de operações criptográficas    │
└─────────────────────────────────────────────────────────┘
```

---

## Camada 1: Proxy MITM

### O que monitora
- Conexões HTTPS para domínios bloqueados
- Injeção de certificado client-side
- Tentativas de acesso não autorizado

### Como implementa

```csharp
public class ProxyService
{
    private async Task OnBeforeRequest(object sender, SessionEventArgs e)
    {
        string host = new Uri(e.HttpClient.Request.Url).Host;

        if (_policy.IsBlocked(host))
        {
            // Log da tentativa bloqueada
            await _audit.LogBlocked(host, "BLOCKLIST");
            
            // Bloquear acesso
            e.Ok(BuildBlockedPage(host));
        }
    }
}
```

### Eventos gerados
| Evento | Descrição |
|--------|-----------|
| `blocked_access` | Domínio bloqueado pelo proxy |
| `cert_injection` | Certificado injetado para domínio permitido |
| `validation_allowed` | Domínio OCSP/CRL permitido |

---

## Camada 2: API Hooking

### O que monitora
- Chamadas a `CryptAcquireCertificatePrivateKey`
- Chamadas a `NCryptSignHash`
- Leitura de propriedades do certificado

### Como implementa (N-API addon C++)

```cpp
// Hook de CryptAcquireCertificatePrivateKey
BOOL WINAPI HookedCryptAcquireCertificatePrivateKey(
    PCCERT_CONTEXT pCert, DWORD dwFlags, void* pvParameters,
    HCRYPTPROV_OR_NCRYPT_KEY_HANDLE* phProv, DWORD* pdwKeySpec,
    BOOL* pfCallerFree)
{
    // Extrair thumbprint
    string thumbprint = ExtractThumbprint(pCert);
    
    // Identificar processo
    string processName = GetProcessName();
    
    // Log
    EmitEvent("key_access", processName, thumbprint);
    
    // Chamar original
    return OriginalCryptAcquireCertificatePrivateKey(
        pCert, dwFlags, pvParameters, phProv, pdwKeySpec, pfCallerFree);
}
```

### Eventos gerados
| Evento | Descrição |
|--------|-----------|
| `key_access` | Alguém acessou a chave privada |
| `signature_operation` | Assinatura foi feita |
| `cert_property_read` | Propriedade do cert lida |

---

## Camada 3: SACL

### O que monitora
- Acesso ao arquivo da chave privada em disco
- Leitura e escrita no arquivo

### Como implementa

```powershell
# Configurar SACL no arquivo da chave
$acl = Get-Acl $keyFile -Audit
$auditRule = New-Object System.Security.AccessControl.FileSystemAuditRule(
    "Everyone", "ReadData", "None", "None", "Success")
$acl.SetAuditRule($auditRule)
Set-Acl $keyFile $acl
```

### Eventos gerados (Security Log)
| Event ID | Descrição |
|----------|-----------|
| 4656 | Handle solicitado ao objeto |
| 4663 | Acesso realizado |
| 4658 | Handle fechado |

---

## Camada 4: Sysmon

### O que monitora
- Criação de processos de criptografia
- Carregamento de DLLs (crypt32.dll, ncrypt.dll)
- Criação de arquivos de chave
- Conexões de rede

### Configuração XML

```xml
<Sysmon schemaversion="4.90">
  <EventFiltering>
    <ProcessCreate onmatch="include">
      <Image condition="contains">certutil.exe</Image>
      <Image condition="contains">openssl.exe</Image>
      <Image condition="contains">chrome.exe</Image>
    </ProcessCreate>
    <ImageLoad onmatch="include">
      <ImageLoaded condition="contains">crypt32.dll</ImageLoaded>
      <ImageLoaded condition="contains">ncrypt.dll</ImageLoaded>
    </ImageLoad>
    <FileCreate onmatch="include">
      <TargetFilename condition="contains">Microsoft\Crypto\Keys</TargetFilename>
    </FileCreate>
  </EventFiltering>
</Sysmon>
```

### Eventos gerados
| Event ID | Descrição |
|----------|-----------|
| 1 | ProcessCreate |
| 7 | ImageLoad |
| 11 | FileCreate |
| 3 | NetworkConnect |

---

## Camada 5: CNG ETW

### O que monitora
- Operações de chave CNG
- Operações NCrypt (abertura, assinatura)
- Operações BCrypt

### Providers

| Provider | GUID |
|----------|------|
| Microsoft-Windows-Crypto-CNG | {E3E0E2F0-C9C5-11E0-8AB9-9EBC4824019B} |
| Microsoft-Windows-Crypto-NCrypt | {E8ED09DC-100C-45E2-9FC8-B53399EC1F70} |

### Como habilitar

```powershell
logman create trace CertGuardETW `
  -p "Microsoft-Windows-Crypto-CNG" 0xFFFFFFFF 0xFF `
  -p "Microsoft-Windows-Crypto-NCrypt" 0xFFFFFFFF 0xFF `
  -o C:\CertGuard\certguard.etl
logman start CertGuardETW
```

### Eventos críticos
| Event ID | Provider | Descrição |
|----------|----------|-----------|
| 16 | NCrypt | Cert-In-Use |
| 9 | NCrypt | Key write succeeded |
| 11 | NCrypt | Delete key succeeded |
| 3 | CNG | Key file operation |

---

## Matriz de Cobertura

| Evento | Proxy | API Hook | SACL | Sysmon | ETW |
|--------|-------|----------|------|--------|-----|
| App envia cert para domínio bloqueado | **BLOQUEIA** | Detecta | - | - | - |
| App acessa chave privada | - | **DETECTA** | Detecta | Detecta | Detecta |
| App faz assinatura | - | **DETECTA** | Detecta | - | Detecta |
| Processo desconhecido acessa cert | - | Detecta | Detecta | Detecta | Detecta |
| Usuário remove cert do store | - | - | Detecta | Detecta | - |
| Acesso ao arquivo da chave | - | - | **DETECTA** | **DETECTA** | Detecta |
| Carregamento de DLLs crypto | - | - | - | **DETECTA** | Detecta |

---

## Performance

| Camada | CPU | Memória | Recomendação |
|--------|-----|---------|--------------|
| Proxy MITM | 2-5% | ~50MB | Sempre ativo |
| API Hooking | 1-3% | ~20MB | Durante sessão |
| SACL | ~0.5% | ~10MB | Sempre ativo |
| Sysmon | ~0.1% | ~15MB | Sempre ativo |
| ETW | ~0.5% | ~30MB | Sob demanda |

---

## Integração com Backend Laravel

### Endpoint de Eventos

```php
// routes/api.php
Route::post('/desktop/certificate-events', 
    [CertificateEventController::class, 'store']);
Route::get('/desktop/certificate-events', 
    [CertificateEventController::class, 'index']);
Route::get('/desktop/certificate-events/stats', 
    [CertificateEventController::class, 'stats']);
```

### Migration

```php
Schema::create('certificate_access_logs', function (Blueprint $table) {
    $table->id();
    $table->uuid('session_id');
    $table->foreignId('user_id')->nullable();
    $table->string('certificate_thumbprint', 40)->nullable();
    $table->string('event_type', 50);
    $table->timestamp('timestamp');
    $table->string('process_name')->nullable();
    $table->integer('process_id')->nullable();
    $table->string('target_domain')->nullable();
    $table->enum('action_taken', ['allowed', 'blocked', 'logged']);
    $table->enum('detection_layer', ['proxy', 'hook', 'sacl', 'sysmon', 'etw']);
    $table->json('metadata')->nullable();
    $table->timestamps();
});
```

---

## Dashboard Filament

- Tela com filtros por data, usuário, certificado, domínio, camada
- Badges de cor: bloqueado (vermelho), logado (amarelo), permitido (verde)
- Exportação CSV para compliance
- Alertas via WebSocket (Laravel Reverb)

---

*Documento: AUDIT-SYSTEM.md*
*Versão: 1.0*
*Data: 14/07/2026*
