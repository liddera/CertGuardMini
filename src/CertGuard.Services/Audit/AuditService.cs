using System.Diagnostics;
using System.Net.Http.Json;
using CertGuard.Core.DTOs;
using CertGuard.Core.Interfaces;

namespace CertGuard.Services.Audit;

public class AuditService : IAuditService
{
    private readonly HttpClient _httpClient;
    private const string EventSource = "CertGuard";

    public AuditService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LogBlockedAsync(string hostname, string processName, string reason)
    {
        EventLog.WriteEntry(EventSource,
            $"BLOQUEADO: {hostname} | Processo: {processName} | Motivo: {reason}",
            EventLogEntryType.Warning);

        try
        {
            await _httpClient.PostAsJsonAsync("/api/desktop/navigation/events",
                new NavigationEventRequest(
                    SessionId: "",
                    EventType: "navigation_violation",
                    Timestamp: DateTime.UtcNow,
                    ProcessName: processName,
                    ProcessId: null,
                    CertificateThumbprint: null,
                    TargetDomain: hostname,
                    ActionTaken: "blocked",
                    DetectionLayer: "proxy",
                    new Dictionary<string, string> { { "reason", reason } }));
        }
        catch
        {
            // Backend sync is best-effort
        }
    }

    public async Task LogCertificateInjectionAsync(string hostname, string thumbprint)
    {
        EventLog.WriteEntry(EventSource,
            $"CERTO INJETADO: {hostname} | Thumbprint: {thumbprint}",
            EventLogEntryType.Information);

        try
        {
            await _httpClient.PostAsJsonAsync("/api/desktop/navigation/events",
                new NavigationEventRequest(
                    SessionId: "",
                    EventType: "cert_injection",
                    Timestamp: DateTime.UtcNow,
                    ProcessName: null,
                    ProcessId: null,
                    CertificateThumbprint: thumbprint,
                    TargetDomain: hostname,
                    ActionTaken: "allowed",
                    DetectionLayer: "proxy",
                    null));
        }
        catch
        {
            // Backend sync is best-effort
        }
    }

    public async Task LogNavigationViolationAsync(string sessionId, string processName, int processId,
        string thumbprint, string targetDomain, string actionTaken, string detectionLayer,
        Dictionary<string, string>? metadata = null)
    {
        EventLog.WriteEntry(EventSource,
            $"VIOLAÇÃO: {targetDomain} | Processo: {processName} | Ação: {actionTaken}",
            EventLogEntryType.Warning);

        try
        {
            await _httpClient.PostAsJsonAsync("/api/desktop/navigation/events",
                new NavigationEventRequest(
                    SessionId: sessionId,
                    EventType: "navigation_violation",
                    Timestamp: DateTime.UtcNow,
                    ProcessName: processName,
                    ProcessId: processId,
                    CertificateThumbprint: thumbprint,
                    TargetDomain: targetDomain,
                    ActionTaken: actionTaken,
                    DetectionLayer: detectionLayer,
                    metadata));
        }
        catch
        {
            // Backend sync is best-effort
        }
    }
}
