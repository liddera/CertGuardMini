namespace CertGuard.Core.DTOs;

public record HeartbeatResponse(string Status, DateTime? ExpiresAt);
