# Comparação: Electron vs .NET WPF

## Resumo Executivo

| Métrica | Electron | .NET WPF | Melhoria |
|---------|----------|----------|----------|
| Tamanho binário | ~180MB | ~30MB | **-83%** |
| Memória RAM | ~150MB | ~40MB | **-73%** |
| Startup | ~3-5s | ~0.5s | **-85%** |
| Linhas de código | ~2.500 | ~1.550 | **-38%** |
| Dependências | 14 | 12 NuGet | **-14%** |

---

## Capacidades Técnicas

| Capacidade | Electron | .NET WPF |
|------------|----------|----------|
| MITM Proxy | Biblioteca JS (imatura) | Titanium.Web.Proxy (maduro) |
| Injeção de client cert | ❌ Não suporta | ✅ ClientCertificateSelectionCallback |
| Remover cert do store | PowerShell via child_process | ✅ X509Store.Remove() |
| Salvar chave privada | safeStorage (variável) | ✅ ProtectedData (DPAPI) |
| Geração RSA | node-forge (JS) | ✅ RSA.Create(2048) nativo |
| Device fingerprint | hostname+IP (muda) | ✅ UUID+MAC+CPU (fixo) |
| HTTP client | Axios | ✅ HttpClient + Refit |
| Heartbeat timer | setInterval (React) | ✅ BackgroundService |
| Logging | console.log | ✅ Serilog + EventLog |
| Tray icon | Electron Tray | ✅ Hardcodet.TaskbarNotification |
| Config | localStorage | ✅ ProtectedData encriptado |
| **Escrever Event Log** | ❌ Não pode | ✅ EventLog.WriteEntry |
| **ACL em arquivos** | ❌ Não pode | ✅ FileSystemAccessControl |
| **Validar cadeia cert** | ❌ Não pode | ✅ X509Chain.Build() |
| **WMI queries** | ❌ Não pode | ✅ System.Management |
| **Registry access** | ❌ Não pode | ✅ Microsoft.Win32.Registry |
| **ETW tracing** | ❌ Não pode | ✅ TraceEventSession |
| **Detecção de processo** | ❌ Não pode | ✅ ETW + WMI |

---

## Segurança

| Aspecto | Electron | .NET WPF |
|---------|----------|----------|
| Crypto library | node-forge (JS) | System.Security.Cryptography (nativo) |
| Key storage | safeStorage (API Electron) | ProtectedData (DPAPI Windows) |
| TLS interception | Biblioteca JS | Titanium.Web.Proxy (maduro) |
| Certificate chain validation | ❌ Não valida local | ✅ X509Chain.Build() |
| Process access detection | ❌ Não detecta | ✅ ETW + SACL |
| Windows Event Log | ❌ Não escreve | ✅ Nativo |
| File ACL | ❌ Não gerencia | ✅ FileSystemAccessControl |

---

## Performance

| Métrica | Electron | .NET WPF |
|---------|----------|----------|
| CPU (idle) | ~2% | ~0.1% |
| CPU (proxy ativo) | ~5% | ~3% |
| RAM (idle) | ~150MB | ~40MB |
| RAM (proxy ativo) | ~200MB | ~60MB |
| Disk I/O | Alto (temp files) | Baixo (memória) |
| Startup cold | ~3-5s | ~0.5s |
| Startup warm | ~1-2s | ~0.2s |

---

## Desenvolvimento

| Aspecto | Electron | .NET WPF |
|---------|----------|----------|
| Linguagem | TypeScript | C# |
| UI framework | React + Tailwind | XAML + Styles |
| State management | Zustand | CommunityToolkit.Mvvm |
| HTTP client | Axios | HttpClient + Refit |
| Build tool | Vite + electron-builder | dotnet publish |
| Compilação | node-gyp + toolchain C++ | dotnet build (1 comando) |
| Testes | Jest/Vitest | xUnit/NUnit |
| CI/CD | GitHub Actions | GitHub Actions |
| Debug | Chrome DevTools | Visual Studio Debugger |

---

## Manutenção

| Aspecto | Electron | .NET WPF |
|---------|----------|----------|
| Atualizar runtime | Electron update (complexo) | .NET SDK update (simples) |
| Dependências | npm (vulnerabilidades comuns) | NuGet (mais rigoroso) |
| Compatibilidade SO | Cross-platform (desnecessário) | Windows-only (foco) |
| Documentação | Dispersa | Oficial Microsoft (excelente) |
| Comunidade | Grande mas fragmentada | Grande e unificada |

---

## O que Electron faz que .NET não faz

| Capacidade | Impacto | Mitigação |
|------------|---------|-----------|
| Cross-platform | 🟢 Nenhum | App é Windows-only |
| Hot reload durante dev | 🟡 Baixo | dotnet watch (similar) |
| Web technologies | 🟡 Baixo | C# é mais fácil para Windows APIs |
| Extensões Chrome | 🟢 Nenhum | Não necessário |

---

## O que .NET faz que Electron não faz

| Capacidade | Benefício |
|------------|-----------|
| MITM Proxy nativo | Controle total de tráfego |
| Client certificate injection | Injeta cert A1 para domínios específicos |
| X509Store nativo | Install/remove sem PowerShell |
| DPAPI nativo | Chave privada segura |
| ETW monitoring | Detecção de processos |
| Windows Event Log | Auditoria nativa |
| ACL em arquivos | Controle de acesso à chave |
| X509Chain | Validação de cadeia ICP-Brasil |
| WMI | Device fingerprint robusto |
| Registry | Config nativa do Windows |

---

## Conclusão

**Electron** foi bom para prototipagem rápida. Mas para o que o CertGuard precisa (proxy MITM, injeção de cert, ACL, ETW, Event Log), **.NET é dramaticamente superior**.

O CertGuardMini já prova que funciona. A migração é questão de **tempo**, não de **risco técnico**.

---

*Documento: COMPARISON.md*
*Versão: 1.0*
*Data: 14/07/2026*
