namespace CertGuard.Core.Models;

public class DomainPolicy
{
    public string[] CertUsageDomains { get; set; } = [];
    public string[] ValidationDomains { get; set; } = [];
    public string[] TimestampDomains { get; set; } = [];
    public string[] BlockedDomains { get; set; } = [];
}
