namespace CertGuard.Core.Models;

public class Device
{
    public int Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? So { get; set; }
    public string? Fingerprint { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
