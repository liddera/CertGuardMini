namespace CertGuard.Core.DTOs;

public record RegisterDeviceRequest(
    string Hostname,
    string IpAddress,
    string So,
    string Fingerprint,
    string PublicKey);
