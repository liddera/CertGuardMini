namespace CertGuard.Core.Models;

public class NavigationPolicy
{
    public int PolicyId { get; set; }
    public string Mode { get; set; } = "allowlist";
    public string ViolationAction { get; set; } = "log_only";
    public string[] CertUsageDomains { get; set; } = [];
    public string[] ValidationDomains { get; set; } = [];
    public string[] TimestampDomains { get; set; } = [];
    public string[] BlockedDomains { get; set; } = [];
}
