using System.Net.Http.Json;
using CertGuard.Core.DTOs;
using CertGuard.Core.Interfaces;
using CertGuard.Core.Models;

namespace CertGuard.Services.Certificates;

public class CertificateService : ICertificateService
{
    private readonly HttpClient _httpClient;
    private readonly ISessionService _sessionService;
    private readonly ICertificateStoreService _certStore;

    public CertificateService(
        HttpClient httpClient,
        ISessionService sessionService,
        ICertificateStoreService certStore)
    {
        _httpClient = httpClient;
        _sessionService = sessionService;
        _certStore = certStore;
    }

    public async Task<List<Certificado>> ListAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<CertificadoListResponse>(
            "/api/desktop/certificados");
        return response?.Certificados ?? [];
    }

    public async Task<Sessao> ActivateAsync(int certificadoId, int deviceId, string? justification)
    {
        var request = new ActivateSessionRequest(certificadoId, deviceId, justification);
        var session = await _sessionService.ActivateAsync(request);

        if (!string.IsNullOrEmpty(session.PfxBase64) && !string.IsNullOrEmpty(session.PfxPassword))
        {
            var pfxBytes = Convert.FromBase64String(session.PfxBase64);
            await _certStore.InstallPfxAsync(pfxBytes, session.PfxPassword);
        }

        return session;
    }

    public async Task DeactivateAsync(string sessionId, string thumbprint)
    {
        await _certStore.RemoveByThumbprintAsync(thumbprint);
        await _sessionService.DeactivateAsync(sessionId);
    }
}

public record CertificadoListResponse(List<Certificado> Certificados);
