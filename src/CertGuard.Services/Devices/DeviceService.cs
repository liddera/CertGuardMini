using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CertGuard.Core.DTOs;
using CertGuard.Core.Interfaces;
using CertGuard.Core.Models;

namespace CertGuard.Services.Devices;

public class DeviceService : IDeviceService
{
    private readonly HttpClient _httpClient;
    private readonly IKeyGenService _keyGenService;
    private int? _cachedDeviceId;

    public DeviceService(HttpClient httpClient, IKeyGenService keyGenService)
    {
        _httpClient = httpClient;
        _keyGenService = keyGenService;
    }

    public async Task<Device> RegisterAsync(RegisterDeviceRequest request)
    {
        if (_cachedDeviceId.HasValue)
        {
            return new Device
            {
                Id = _cachedDeviceId.Value,
                Hostname = request.Hostname,
                IpAddress = request.IpAddress,
                So = request.So,
                Fingerprint = request.Fingerprint,
                IsActive = true
            };
        }

        var response = await _httpClient.PostAsJsonAsync("/api/desktop/devices", request);
        response.EnsureSuccessStatusCode();

        var result = (await response.Content.ReadFromJsonAsync<RegisterDeviceResponse>())!;
        _cachedDeviceId = result.DeviceId;

        return new Device
        {
            Id = result.DeviceId,
            Hostname = request.Hostname,
            IpAddress = request.IpAddress,
            So = request.So,
            Fingerprint = result.Fingerprint,
            IsActive = result.IsActive
        };
    }

    public async Task<List<Device>> ListAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<DeviceListResponse>("/api/desktop/devices");
        return response?.Devices ?? [];
    }

    public async Task DeactivateAsync(int deviceId)
    {
        var response = await _httpClient.DeleteAsync($"/api/desktop/devices/{deviceId}");
        response.EnsureSuccessStatusCode();
    }

    public static string GenerateFingerprint(string hostname)
    {
        var combined = $"{hostname}-{Environment.MachineName}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public record DeviceListResponse(List<Device> Devices);
