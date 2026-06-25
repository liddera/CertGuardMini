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

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<DomainRule>? DomainBlocked;

    public void LoadCertificate(CertificateInfo cert)
    {
        try
        {
            if (!string.IsNullOrEmpty(cert.PfxBase64) && !string.IsNullOrEmpty(cert.Password))
            {
                var pfxBytes = Convert.FromBase64String(cert.PfxBase64);
                _certificate = new X509Certificate2(
                    pfxBytes,
                    cert.Password,
                    X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

                cert.Thumbprint = _certificate.Thumbprint;
            }
            else
            {
                _certificate = null;
            }

            _currentCert = cert;
            cert.IsActive = true;
            cert.ActivatedAt = DateTime.Now;

            StatusChanged?.Invoke(this, $"Certificado '{cert.DisplayName}' carregado na memória");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Erro ao carregar certificado: {ex.Message}");
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

        LoadCertificate(cert);
        RebuildDomainRules();
    }

    public bool IsDomainAllowed(string domain)
    {
        var normalizedDomain = domain.ToLower().Trim();

        var blocked = _domainRules
            .Where(r => r.IsBlocked)
            .Any(r => normalizedDomain.Contains(r.Domain.ToLower()));

        if (blocked)
        {
            var rule = _domainRules.First(r => r.IsBlocked && normalizedDomain.Contains(r.Domain.ToLower()));
            DomainBlocked?.Invoke(this, rule);
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
        _domainRules.RemoveAll(r =>
            r.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));

        _domainRules.Add(new DomainRule
        {
            Domain = domain,
            Label = label,
            IsBlocked = isBlocked
        });
    }

    public void RemoveDomainRule(string domain)
    {
        _domainRules.RemoveAll(r =>
            r.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
    }

    public void RebuildDomainRules()
    {
        _domainRules.Clear();

        if (_currentCert is null) return;

        foreach (var domain in _currentCert.AllowedDomains)
        {
            _domainRules.Add(new DomainRule
            {
                Domain = domain,
                Label = domain,
                IsBlocked = false
            });
        }

        foreach (var domain in _currentCert.BlockedDomains)
        {
            _domainRules.Add(new DomainRule
            {
                Domain = domain,
                Label = domain,
                IsBlocked = true
            });
        }
    }

    public X509Certificate2? GetCertificate()
    {
        return _certificate;
    }

    public void Unload()
    {
        _certificate?.Dispose();
        _certificate = null;

        if (_currentCert is not null)
        {
            _currentCert.IsActive = false;
            _currentCert = null;
        }

        _domainRules.Clear();
        StatusChanged?.Invoke(this, "Certificado descarregado da memória");
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }
}
