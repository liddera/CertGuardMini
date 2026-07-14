using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CertGuard.Core.DTOs;
using CertGuard.Core.Interfaces;

namespace CertGuard.Services.Crypto;

public class KeyGenService : IKeyGenService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CertGuard-v1");

    public Task<(string PublicKey, byte[] EncryptedPrivateKey)> GenerateKeyPairAsync()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var privateKeyBytes = rsa.ExportPkcs8PrivateKey();

        var encryptedPrivateKey = ProtectedData.Protect(
            privateKeyBytes, Entropy, DataProtectionScope.CurrentUser);

        return Task.FromResult((publicKey, encryptedPrivateKey));
    }

    public Task<string> GetPublicKeyFingerprintAsync()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(publicKey));
        return Task.FromResult(Convert.ToHexString(hash).ToLowerInvariant());
    }
}
