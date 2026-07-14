namespace CertGuard.Core.DTOs;

public record ActivateSessionRequest(
    int CertificadoId,
    int DeviceId,
    string? Justification);
