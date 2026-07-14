using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CertGuard.Core.Interfaces;

namespace CertGuard.Services.Sessions;

public class HeartbeatService : BackgroundService
{
    private readonly ILogger<HeartbeatService> _logger;
    private readonly ISessionService _sessionService;
    private readonly ICertificateStoreService _certStore;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    private string? _sessionId;
    private string? _thumbprint;

    public HeartbeatService(
        ILogger<HeartbeatService> logger,
        ISessionService sessionService,
        ICertificateStoreService certStore)
    {
        _logger = logger;
        _sessionService = sessionService;
        _certStore = certStore;
    }

    public void SetSession(string sessionId, string thumbprint)
    {
        _sessionId = sessionId;
        _thumbprint = thumbprint;
    }

    public void ClearSession()
    {
        _sessionId = null;
        _thumbprint = null;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_sessionId != null)
            {
                try
                {
                    var response = await _sessionService.HeartbeatAsync(_sessionId);

                    if (response.Status is "expired" or "revoked")
                    {
                        _logger.LogWarning("Session {Status}, cleaning up", response.Status);
                        await CleanupAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Heartbeat failed");
                }
            }

            await Task.Delay(_interval, ct);
        }
    }

    private async Task CleanupAsync()
    {
        if (_thumbprint != null)
            await _certStore.RemoveByThumbprintAsync(_thumbprint);

        if (_sessionId != null)
            await _sessionService.DeactivateAsync(_sessionId);

        _sessionId = null;
        _thumbprint = null;
    }
}
