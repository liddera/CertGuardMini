using System.Net.Http.Json;
using CertGuard.Core.DTOs;
using CertGuard.Core.Interfaces;
using CertGuard.Core.Models;

namespace CertGuard.Services.Sessions;

public class SessionService : ISessionService
{
    private readonly HttpClient _httpClient;

    public SessionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Sessao> ActivateAsync(ActivateSessionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/desktop/sessoes", request);
        response.EnsureSuccessStatusCode();

        var result = (await response.Content.ReadFromJsonAsync<Sessao>())!;
        return result;
    }

    public async Task<HeartbeatResponse> HeartbeatAsync(string sessionId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/desktop/heartbeat", new { session_id = sessionId });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<HeartbeatResponse>())!;
    }

    public async Task DeactivateAsync(string sessionId)
    {
        var response = await _httpClient.DeleteAsync($"/api/desktop/sessoes/{sessionId}");
        response.EnsureSuccessStatusCode();
    }
}
