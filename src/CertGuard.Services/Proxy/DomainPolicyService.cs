using System.Collections.Concurrent;
using CertGuard.Core.Interfaces;
using CertGuard.Core.Models;

namespace CertGuard.Services.Proxy;

public class DomainPolicyService : IDomainPolicyService
{
    private readonly ConcurrentDictionary<string, byte> _certUsage = new();
    private readonly ConcurrentDictionary<string, byte> _validation = new();
    private readonly ConcurrentDictionary<string, byte> _timestamp = new();
    private readonly ConcurrentDictionary<string, byte> _blocked = new();

    public void LoadPolicy(NavigationPolicy policy)
    {
        _certUsage.Clear();
        foreach (var d in policy.CertUsageDomains) _certUsage[d] = 1;

        _validation.Clear();
        foreach (var d in policy.ValidationDomains) _validation[d] = 1;

        _timestamp.Clear();
        foreach (var d in policy.TimestampDomains) _timestamp[d] = 1;

        _blocked.Clear();
        foreach (var d in policy.BlockedDomains) _blocked[d] = 1;
    }

    public bool IsAllowedForInterception(string host) =>
        MatchDomain(host, _certUsage) || MatchDomain(host, _validation) || MatchDomain(host, _timestamp);

    public bool CanUseCertificateA1(string host) =>
        MatchDomain(host, _certUsage);

    public bool IsValidationDomain(string host) =>
        MatchDomain(host, _validation) || MatchDomain(host, _timestamp);

    public bool IsBlocked(string host) =>
        MatchDomain(host, _blocked);

    private static bool MatchDomain(string host, ConcurrentDictionary<string, byte> list)
    {
        if (list.ContainsKey(host)) return true;

        foreach (var pattern in list.Keys)
        {
            if (pattern.StartsWith("*."))
            {
                var suffix = pattern[1..];
                if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }
}
