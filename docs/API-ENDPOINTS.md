# Mapa de Endpoints da API

## Visão Geral

Todos os endpoints são consumidos pelo CertGuard Desktop via HTTP. Base URL: `https://homolog.lidderaplus.com.br/api`

---

## Endpoints de Autenticação

### POST /api/desktop/login

**O que faz:** Autentica o operador e retorna token Sanctum.

**Request:**
```json
{
  "email": "operator@empresa.com",
  "password": "senha123"
}
```

**Response 201:**
```json
{
  "token": "1|abc123...",
  "user": {
    "id": 1,
    "name": "João Silva",
    "email": "operator@empresa.com"
  }
}
```

**Response 422:**
```json
{
  "message": "The given data was invalid.",
  "errors": {
    "email": ["The selected email is invalid."]
  }
}
```

**Uso no .NET:**
```csharp
public async Task<LoginResponse> LoginAsync(string email, string password)
{
    var response = await _httpClient.PostAsJsonAsync("/api/desktop/login", 
        new { email, password });
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<LoginResponse>();
}
```

---

### POST /api/desktop/logout

**O que faz:** Revoga o token Sanctum atual.

**Headers:** `Authorization: Bearer {token}`

**Response 200:**
```json
{
  "message": "Logged out"
}
```

---

### GET /api/desktop/me

**O que faz:** Retorna os dados do usuário autenticado.

**Headers:** `Authorization: Bearer {token}`

**Response 200:**
```json
{
  "id": 1,
  "name": "João Silva",
  "email": "operator@empresa.com"
}
```

---

## Endpoints de Dispositivos

### POST /api/desktop/devices

**O que faz:** Registra ou atualiza o dispositivo do operador.

**Headers:** `Authorization: Bearer {token}`

**Request:**
```json
{
  "hostname": "PC-JOAO-01",
  "ip_address": "192.168.1.100",
  "so": "win32 x64",
  "fingerprint": "a1b2c3d4e5f6...",
  "public_key": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBg..."
}
```

**Response 201:**
```json
{
  "device_id": 1,
  "fingerprint": "a1b2c3d4e5f6...",
  "is_active": true
}
```

**Uso no .NET:**
```csharp
public async Task<DeviceResponse> RegisterAsync(RegisterDeviceRequest request)
{
    var response = await _httpClient.PostAsJsonAsync("/api/desktop/devices", request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<DeviceResponse>();
}
```

---

### GET /api/desktop/devices

**O que faz:** Lista todos os dispositivos do usuário.

**Headers:** `Authorization: Bearer {token}`

**Response 200:**
```json
{
  "devices": [
    {
      "id": 1,
      "hostname": "PC-JOAO-01",
      "ip_address": "192.168.1.100",
      "so": "win32 x64",
      "fingerprint": "a1b2c3d4e5f6...",
      "is_active": true,
      "last_seen_at": "2026-07-14T10:30:00Z"
    }
  ]
}
```

---

### DELETE /api/desktop/devices/{device_id}

**O que faz:** Desativa um dispositivo.

**Headers:** `Authorization: Bearer {token}`

**Response 200:**
```json
{
  "success": true
}
```

**Response 403:**
```json
{
  "message": "Unauthorized"
}
```

---

## Endpoints de Certificados

### GET /api/desktop/certificados

**O que faz:** Lista os certificados visíveis para o usuário (direto + grupo).

**Headers:** `Authorization: Bearer {token}`

**Response 200:**
```json
{
  "certificados": [
    {
      "id": 1,
      "apelido": "Certificado Empresa",
      "empresa": "Alvras Ltda",
      "cnpj": "12.345.678/0001-90",
      "status": "vigente",
      "data_vencimento": "2027-12-31",
      "requires_justification": true,
      "session_ttl_minutes": 60,
      "allowed_weekdays": ["SEG","TER","QUA","QUI","SEX"],
      "allowed_time_start": "08:00",
      "allowed_time_end": "18:00"
    }
  ]
}
```

---

## Endpoints de Sessões

### POST /api/desktop/sessoes

**O que faz:** Ativa uma sessão e emite o certificado PFX.

**Headers:** `Authorization: Bearer {token}`

**Request:**
```json
{
  "certificado_id": 1,
  "device_id": 1,
  "justification": "Acesso ao e-CAC para declaração"
}
```

**Response 201:**
```json
{
  "session_id": "abc123...",
  "session_code": "550e8400-e29b-41d4-a716-446655440000",
  "certificado_id": 1,
  "cnpj": "12.345.678/0001-90",
  "common_name": "Empresa Teste Ltda",
  "expires_at": "2026-07-14T11:30:00Z",
  "pfx_base64": "MIIJqgIBAAK...",
  "pfx_password": "abc123..."
}
```

**Response 422:**
```json
{
  "success": false,
  "message": "Usuário não possui permissão para este certificado"
}
```

**Uso no .NET:**
```csharp
public async Task<SessionResponse> ActivateAsync(ActivateSessionRequest request)
{
    var response = await _httpClient.PostAsJsonAsync("/api/desktop/sessoes", request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<SessionResponse>();
}
```

---

### POST /api/desktop/heartbeat

**O que faz:** Reporta status da sessão (não renova TTL).

**Headers:** `Authorization: Bearer {token}`

**Request:**
```json
{
  "session_id": "abc123..."
}
```

**Response 200 (ativa):**
```json
{
  "status": "active",
  "expires_at": "2026-07-14T11:30:00Z"
}
```

**Response 200 (expirada):**
```json
{
  "status": "expired",
  "expires_at": "2026-07-14T11:30:00Z"
}
```

**Response 200 (revogada):**
```json
{
  "status": "revoked",
  "expires_at": null
}
```

---

### DELETE /api/desktop/sessoes/{session_id}

**O que faz:** Encerra uma sessão.

**Headers:** `Authorization: Bearer {token}`

**Response 200:**
```json
{
  "success": true
}
```

**Response 403:**
```json
{
  "message": "Unauthorized"
}
```

---

## Endpoints Novos (a implementar)

### GET /api/desktop/navigation/policy

**O que faz:** Retorna a política de domínios para a sessão.

**Headers:** `Authorization: Bearer {token}`

**Query:** `?session_id={session_id}`

**Response 200:**
```json
{
  "policy_id": 1,
  "mode": "allowlist",
  "violation_action": "log_only",
  "cert_usage_domains": [
    "*.receita.fazenda.gov.br",
    "*.sefaz.rs.gov.br"
  ],
  "validation_domains": [
    "acraiz.icpbrasil.gov.br",
    "*.certisign.com.br"
  ],
  "timestamp_domains": [
    "timestamp.digicert.com"
  ],
  "blocked_domains": []
}
```

---

### POST /api/desktop/navigation/events

**O que faz:** Registra uma violação de navegação.

**Headers:** `Authorization: Bearer {token}`

**Request:**
```json
{
  "session_id": "abc123...",
  "event_type": "navigation_violation",
  "timestamp": "2026-07-14T10:30:00Z",
  "process_name": "chrome.exe",
  "process_id": 1234,
  "certificate_thumbprint": "AB12CD34...",
  "target_domain": "site-bloqueado.com",
  "action_taken": "blocked",
  "detection_layer": "proxy",
  "metadata": {
    "reason": "NOT_IN_ALLOWLIST"
  }
}
```

**Response 201:**
```json
{
  "id": 1
}
```

---

### GET /api/desktop/certificate-events

**O que faz:** Lista eventos de acesso ao certificado.

**Headers:** `Authorization: Bearer {token}`

**Query:**
- `start_date` — Data inicial (ISO8601)
- `end_date` — Data final (ISO8601)
- `user_id` — Filtrar por usuário
- `certificate_thumbprint` — Filtrar por certificado
- `detection_layer` — Filtrar por camada (proxy/hook/sacl/sysmon/etw)
- `event_type` — Filtrar por tipo
- `action_taken` — Filtrar por ação (allowed/blocked/logged)
- `page` — Página (default: 1)
- `per_page` — Itens por página (default: 50)

**Response 200:**
```json
{
  "data": [
    {
      "id": 1,
      "session_id": "abc123...",
      "user_id": 1,
      "certificate_thumbprint": "AB12CD34...",
      "event_type": "navigation_violation",
      "timestamp": "2026-07-14T10:30:00Z",
      "process_name": "chrome.exe",
      "process_id": 1234,
      "target_domain": "site-bloqueado.com",
      "action_taken": "blocked",
      "detection_layer": "proxy",
      "metadata": {
        "reason": "NOT_IN_ALLOWLIST"
      },
      "ip_address": "192.168.1.100",
      "computer_name": "PC-JOAO-01"
    }
  ],
  "links": {
    "first": "?page=1",
    "last": "?page=5",
    "prev": null,
    "next": "?page=2"
  },
  "meta": {
    "current_page": 1,
    "last_page": 5,
    "per_page": 50,
    "total": 250
  }
}
```

---

### GET /api/desktop/certificate-events/stats

**O que faz:** Retorna estatísticas dos eventos.

**Headers:** `Authorization: Bearer {token}`

**Response 200:**
```json
{
  "stats": [
    {
      "event_type": "navigation_violation",
      "detection_layer": "proxy",
      "action_taken": "blocked",
      "count": 45
    },
    {
      "event_type": "key_access",
      "detection_layer": "hook",
      "action_taken": "logged",
      "count": 120
    }
  ],
  "violation_count_30d": 45
}
```

---

## Fluxo Completo: Login → Ativação → Heartbeat → Desativação

```
1. Login
   POST /api/desktop/login
   → { token, user }

2. Registrar Device (se necessário)
   POST /api/desktop/devices
   → { device_id }

3. Listar Certificados
   GET /api/desktop/certificados
   → { certificados[] }

4. Ativar Sessão
   POST /api/desktop/sessoes
   → { session_id, pfx_base64, pfx_password, expires_at }

5. Heartbeat (a cada 30s)
   POST /api/desktop/heartbeat
   → { status, expires_at }

6. Listar Eventos (admin)
   GET /api/desktop/certificate-events
   → { data[], meta }

7. Enviar Evento de Violação
   POST /api/desktop/navigation/events
   → { id }

8. Desativar Sessão
   DELETE /api/desktop/sessoes/{session_id}
   → { success }

9. Logout
   POST /api/desktop/logout
   → { message }
```

---

## Autenticação

Todos os endpoints (exceto `/login`) requerem header:
```
Authorization: Bearer {token}
```

O token é obtido no `/login` e válido até ser revogado no `/logout`.

---

*Documento: API-ENDPOINTS.md*
*Versão: 1.0*
*Data: 14/07/2026*
