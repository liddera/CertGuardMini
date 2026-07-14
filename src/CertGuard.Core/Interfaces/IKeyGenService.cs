namespace CertGuard.Core.Interfaces;

public interface IKeyGenService
{
    Task<(string PublicKey, byte[] EncryptedPrivateKey)> GenerateKeyPairAsync();
    Task<string> GetPublicKeyFingerprintAsync();
}
