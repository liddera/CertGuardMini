namespace CertGuard.Core.Models;

public class Certificado
{
    public int Id { get; set; }
    public string Apelido { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Cnpj { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DataVencimento { get; set; }
    public bool RequiresJustification { get; set; }
    public int SessionTtlMinutes { get; set; }
    public string[] AllowedWeekdays { get; set; } = [];
    public string? AllowedTimeStart { get; set; }
    public string? AllowedTimeEnd { get; set; }
}
