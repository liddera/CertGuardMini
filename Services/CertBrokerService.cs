using System.IO;
using System.Security.Cryptography.X509Certificates;
using CertGuardMini.Models;

namespace CertGuardMini.Services;

public class CertBrokerService : IDisposable
{
    private X509Certificate2? _certificate;
    private CertificateInfo? _currentCert;
    private readonly List<DomainRule> _domainRules = [];

    public bool IsLoaded => _certificate is not null;
    public CertificateInfo? CurrentCertificate => _currentCert;
    public IReadOnlyList<DomainRule> DomainRules => _domainRules.AsReadOnly();
    public X509Certificate2? Certificate => _certificate;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<DomainRule>? DomainBlocked;

    public void LoadFromPfxFile(string filePath, string password)
    {
        try
        {
            var pfxBytes = File.ReadAllBytes(filePath);
            LoadFromPfxBytes(pfxBytes, password, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Erro ao ler arquivo: {ex.Message}");
            throw;
        }
    }

    public void LoadFromPfxBytes(byte[] pfxBytes, string password, string displayName = "Certificado")
    {
        try
        {
            _certificate?.Dispose();

            _certificate = new X509Certificate2(
                pfxBytes,
                password,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

            _currentCert = new CertificateInfo
            {
                DisplayName = displayName,
                Thumbprint = _certificate.Thumbprint,
                IsActive = true,
                ActivatedAt = DateTime.Now,
                AllowedDomains =
                [
                    "download.dfe.sefin.ro.gov.br",
                    "sistemas.receita.fazenda.gov.br",
                    "portal.esocial.gov.br",
                    "sped.fazenda.gov.br"
                ],
                BlockedDomains = ["google.com", "facebook.com"]
            };

            RebuildDomainRules();

            var subject = _certificate.SubjectName.Name ?? "Desconhecido";
            var validUntil = _certificate.NotAfter.ToString("dd/MM/yyyy");

            StatusChanged?.Invoke(this,
                $"Certificado REAL carregado!\n" +
                $"  Titular: {subject}\n" +
                $"  Thumbprint: {_certificate.Thumbprint[..16]}...\n" +
                $"  Validade: {validUntil}\n" +
                $"  Tamanho: {pfxBytes.Length} bytes");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Erro ao carregar PFX: {ex.Message}");
            throw;
        }
    }

    public void LoadSimulatedCertificate(string displayName = "Certificado Simulado")
    {
        var cert = new CertificateInfo
        {
            DisplayName = displayName,
            Thumbprint = $"SIM-{Guid.NewGuid():N}"[..16].ToUpper(),
            AllowedDomains =
            [
                "download.dfe.sefin.ro.gov.br",
                "sistemas.receita.fazenda.gov.br",
                "portal.esocial.gov.br",
                "sped.fazenda.gov.br"
            ],
            BlockedDomains = ["google.com", "facebook.com"]
        };

        _certificate?.Dispose();
        _certificate = null;
        _currentCert = cert;
        cert.IsActive = true;
        cert.ActivatedAt = DateTime.Now;

        RebuildDomainRules();
        StatusChanged?.Invoke(this, $"Certificado SIMULADO carregado (sem PFX real)");
    }

    public bool IsDomainAllowed(string domain)
    {
        var normalizedDomain = domain.ToLower().Trim();

        var blocked = _domainRules
            .Where(r => r.IsBlocked)
            .Any(r => normalizedDomain.Contains(r.Domain.ToLower()));

        if (blocked)
        {
            DomainBlocked?.Invoke(this, _domainRules.First(r => r.IsBlocked && normalizedDomain.Contains(r.Domain.ToLower())));
            return false;
        }

        if (_domainRules.Count == 0)
            return true;

        return _domainRules
            .Where(r => !r.IsBlocked)
            .Any(r => normalizedDomain.Contains(r.Domain.ToLower()));
    }

    public void AddDomainRule(string domain, string label, bool isBlocked)
    {
        _domainRules.RemoveAll(r => r.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
        _domainRules.Add(new DomainRule { Domain = domain, Label = label, IsBlocked = isBlocked });
    }

    public void RemoveDomainRule(string domain)
    {
        _domainRules.RemoveAll(r => r.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
    }

    public void RebuildDomainRules()
    {
        _domainRules.Clear();
        if (_currentCert is null) return;

        foreach (var domain in _currentCert.AllowedDomains)
            _domainRules.Add(new DomainRule { Domain = domain, Label = domain, IsBlocked = false });

        foreach (var domain in _currentCert.BlockedDomains)
            _domainRules.Add(new DomainRule { Domain = domain, Label = domain, IsBlocked = true });
    }

    public X509Certificate2? GetCertificate() => _certificate;

    public void Unload()
    {
        _certificate?.Dispose();
        _certificate = null;
        if (_currentCert is not null) { _currentCert.IsActive = false; _currentCert = null; }
        _domainRules.Clear();
        StatusChanged?.Invoke(this, "Certificado descarregado da memória");
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }
}
