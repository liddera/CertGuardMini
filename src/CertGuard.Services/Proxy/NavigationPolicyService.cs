using System.Net.Http.Json;
using CertGuard.Core.DTOs;
using CertGuard.Core.Interfaces;
using CertGuard.Core.Models;

namespace CertGuard.Services.Proxy;

public class NavigationPolicyService
{
    private readonly HttpClient _httpClient;
    private readonly IDomainPolicyService _domainPolicy;

    public NavigationPolicyService(HttpClient httpClient, IDomainPolicyService domainPolicy)
    {
        _httpClient = httpClient;
        _domainPolicy = domainPolicy;
    }

    public async Task FetchAndLoadAsync(string sessionId)
    {
        var response = await _httpClient.GetFromJsonAsync<NavigationPolicyResponse>(
            $"/api/desktop/navigation/policy?session_id={sessionId}");

        if (response != null)
        {
            var policy = new NavigationPolicy
            {
                PolicyId = response.PolicyId,
                Mode = response.Mode,
                ViolationAction = response.ViolationAction,
                CertUsageDomains = response.CertUsageDomains,
                ValidationDomains = response.ValidationDomains,
                TimestampDomains = response.TimestampDomains,
                BlockedDomains = response.BlockedDomains
            };

            _domainPolicy.LoadPolicy(policy);
        }
    }
}
