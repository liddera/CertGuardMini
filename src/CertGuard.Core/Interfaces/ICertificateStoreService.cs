namespace CertGuard.Core.Interfaces;

public interface ICertificateStoreService
{
    Task<string> InstallPfxAsync(byte[] pfxBytes, string password);
    Task RemoveByThumbprintAsync(string thumbprint);
    Task<bool> ExistsAsync(string thumbprint);
    Task CleanupOrphansAsync();
}
