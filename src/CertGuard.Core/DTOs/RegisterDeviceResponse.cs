namespace CertGuard.Core.DTOs;

public record RegisterDeviceResponse(
    int DeviceId,
    string Fingerprint,
    bool IsActive);
