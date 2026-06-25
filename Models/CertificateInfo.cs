namespace CertGuardMini.Models;

public class CertificateInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public string DisplayName { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
    public string? PfxBase64 { get; set; }
    public string? Password { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public List<string> AllowedDomains { get; set; } = [];
    public List<string> BlockedDomains { get; set; } = [];
}

public class DomainRule
{
    public string Domain { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
}
