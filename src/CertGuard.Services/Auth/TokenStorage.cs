using System.Security.Cryptography;
using System.Text;
using CertGuard.Core.Interfaces;

namespace CertGuard.Services.Auth;

public class TokenStorage : ITokenStorage
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CertGuard-v1");
    private const string TokenFilePath = "certguard_token.dat";

    public async Task SaveTokenAsync(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var encrypted = ProtectedData.Protect(tokenBytes, Entropy, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(TokenFilePath, encrypted);
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!File.Exists(TokenFilePath))
            return null;

        var encrypted = await File.ReadAllBytesAsync(TokenFilePath);
        var decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }

    public Task ClearTokenAsync()
    {
        if (File.Exists(TokenFilePath))
            File.Delete(TokenFilePath);
        return Task.CompletedTask;
    }
}
