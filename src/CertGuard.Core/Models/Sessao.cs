namespace CertGuard.Core.Models;

public class Sessao
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionCode { get; set; } = string.Empty;
    public int CertificadoId { get; set; }
    public string? Cnpj { get; set; }
    public string? CommonName { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? PfxBase64 { get; set; }
    public string? PfxPassword { get; set; }
}
