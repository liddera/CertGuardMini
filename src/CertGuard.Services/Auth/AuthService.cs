using System.Net.Http.Json;
using CertGuard.Core.DTOs;
using CertGuard.Core.Interfaces;
using CertGuard.Core.Models;

namespace CertGuard.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorage _tokenStorage;

    public AuthService(HttpClient httpClient, ITokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/desktop/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var result = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        await _tokenStorage.SaveTokenAsync(result.Token);
        return result;
    }

    public async Task LogoutAsync()
    {
        await _httpClient.PostAsync("/api/desktop/logout", null);
        await _tokenStorage.ClearTokenAsync();
    }

    public async Task<User> GetMeAsync()
    {
        return (await _httpClient.GetFromJsonAsync<User>("/api/desktop/me"))!;
    }
}
