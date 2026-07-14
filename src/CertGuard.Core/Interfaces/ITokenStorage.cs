namespace CertGuard.Core.Interfaces;

public interface ITokenStorage
{
    Task SaveTokenAsync(string token);
    Task<string?> GetTokenAsync();
    Task ClearTokenAsync();
}
