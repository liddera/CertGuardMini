using CertGuard.Core.DTOs;
using CertGuard.Core.Models;

namespace CertGuard.Core.Interfaces;

public interface ICertificateService
{
    Task<List<Certificado>> ListAsync();
    Task<Sessao> ActivateAsync(int certificadoId, int deviceId, string? justification);
    Task DeactivateAsync(string sessionId, string thumbprint);
}
