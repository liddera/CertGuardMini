using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CertGuard.Core.Interfaces;

namespace CertGuard.Services.Crypto;

public class CertificateStoreService : ICertificateStoreService
{
    public async Task<string> InstallPfxAsync(byte[] pfxBytes, string password)
    {
        return await Task.Run(() =>
        {
            var cert = new X509Certificate2(
                pfxBytes, password,
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();

            return cert.Thumbprint;
        });
    }

    public async Task RemoveByThumbprintAsync(string thumbprint)
    {
        await Task.Run(() =>
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var found = store.Certificates.Find(
                X509FindType.FindByThumbprint, thumbprint, false);

            foreach (var cert in found)
                store.Remove(cert);

            store.Close();
        });
    }

    public async Task<bool> ExistsAsync(string thumbprint)
    {
        return await Task.Run(() =>
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var found = store.Certificates.Find(
                X509FindType.FindByThumbprint, thumbprint, false);

            store.Close();
            return found.Count > 0;
        });
    }

    public async Task CleanupOrphansAsync()
    {
        await Task.Run(() =>
        {
            var systemSubjects = new[] { "CN=Microsoft", "CN=DigiCert", "CN=GlobalSign" };

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var toRemove = store.Certificates
                .Where(c => c.HasPrivateKey &&
                    !systemSubjects.Any(s => c.Subject.Contains(s)))
                .ToList();

            foreach (var cert in toRemove)
                store.Remove(cert);

            store.Close();
        });
    }
}
