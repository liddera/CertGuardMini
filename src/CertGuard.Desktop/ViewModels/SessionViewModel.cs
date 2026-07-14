using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CertGuard.Core.Interfaces;

namespace CertGuard.Desktop.ViewModels;

public partial class SessionViewModel : ObservableObject
{
    private readonly ISessionService _sessionService;
    private readonly ICertificateStoreService _certStore;
    private readonly DispatcherTimer _countdownTimer;

    [ObservableProperty] private string _sessionId = string.Empty;
    [ObservableProperty] private string _thumbprint = string.Empty;
    [ObservableProperty] private DateTime _expiresAt;
    [ObservableProperty] private TimeSpan _remaining;
    [ObservableProperty] private bool _showExpiryDialog;
    [ObservableProperty] private bool _isActive;

    public SessionViewModel(ISessionService sessionService, ICertificateStoreService certStore)
    {
        _sessionService = sessionService;
        _certStore = certStore;

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += CountdownTimer_Tick;
    }

    public void StartSession(string sessionId, string thumbprint, DateTime expiresAt)
    {
        SessionId = sessionId;
        Thumbprint = thumbprint;
        ExpiresAt = expiresAt;
        IsActive = true;
        ShowExpiryDialog = false;
        _countdownTimer.Start();
        UpdateRemaining();
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        _countdownTimer.Stop();
        ShowExpiryDialog = false;
        IsActive = false;

        await _certStore.RemoveByThumbprintAsync(Thumbprint);
        await _sessionService.DeactivateAsync(SessionId);
    }

    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        UpdateRemaining();
    }

    private void UpdateRemaining()
    {
        Remaining = ExpiresAt - DateTime.UtcNow;

        if (Remaining.TotalSeconds <= 0)
        {
            _countdownTimer.Stop();
            ShowExpiryDialog = false;
            _ = DeactivateAsync();
        }
        else if (Remaining.TotalSeconds <= 300)
        {
            ShowExpiryDialog = true;
        }
    }
}
