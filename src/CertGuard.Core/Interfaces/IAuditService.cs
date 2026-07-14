namespace CertGuard.Core.Interfaces;

public interface IAuditService
{
    Task LogBlockedAsync(string hostname, string processName, string reason);
    Task LogCertificateInjectionAsync(string hostname, string thumbprint);
    Task LogNavigationViolationAsync(string sessionId, string processName, int processId,
        string thumbprint, string targetDomain, string actionTaken, string detectionLayer,
        Dictionary<string, string>? metadata = null);
}
