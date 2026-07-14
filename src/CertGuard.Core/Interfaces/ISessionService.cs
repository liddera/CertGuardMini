using CertGuard.Core.DTOs;

namespace CertGuard.Core.Interfaces;

public interface ISessionService
{
    Task<Sessao> ActivateAsync(ActivateSessionRequest request);
    Task<HeartbeatResponse> HeartbeatAsync(string sessionId);
    Task DeactivateAsync(string sessionId);
}
