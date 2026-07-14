namespace CertGuard.Core.DTOs;

public record NavigationPolicyResponse(
    int PolicyId,
    string Mode,
    string ViolationAction,
    string[] CertUsageDomains,
    string[] ValidationDomains,
    string[] TimestampDomains,
    string[] BlockedDomains);
