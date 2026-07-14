using CertGuard.Core.Models;

namespace CertGuard.Core.Interfaces;

public interface IDomainPolicyService
{
    void LoadPolicy(NavigationPolicy policy);
    bool IsAllowedForInterception(string host);
    bool CanUseCertificateA1(string host);
    bool IsValidationDomain(string host);
    bool IsBlocked(string host);
}
