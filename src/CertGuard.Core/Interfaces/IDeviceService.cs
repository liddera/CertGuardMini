using CertGuard.Core.DTOs;
using CertGuard.Core.Models;

namespace CertGuard.Core.Interfaces;

public interface IDeviceService
{
    Task<Device> RegisterAsync(RegisterDeviceRequest request);
    Task<List<Device>> ListAsync();
    Task DeactivateAsync(int deviceId);
}
