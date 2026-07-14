namespace CertGuard.Core.DTOs;

public record NavigationEventRequest(
    string SessionId,
    string EventType,
    DateTime Timestamp,
    string? ProcessName,
    int? ProcessId,
    string? CertificateThumbprint,
    string TargetDomain,
    string ActionTaken,
    string DetectionLayer,
    Dictionary<string, string>? Metadata);
