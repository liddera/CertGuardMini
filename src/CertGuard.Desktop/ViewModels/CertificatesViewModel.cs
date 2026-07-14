using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CertGuard.Core.Interfaces;
using CertGuard.Core.Models;

namespace CertGuard.Desktop.ViewModels;

public partial class CertificatesViewModel : ObservableObject
{
    private readonly ICertificateService _certificateService;
    private readonly IDeviceService _deviceService;
    private readonly IDomainPolicyService _domainPolicy;
    private readonly ISessionService _sessionService;
    private readonly ICertificateStoreService _certStore;
    private readonly DispatcherTimer _countdownTimer;
    private readonly DispatcherTimer _heartbeatTimer;
    private DateTime _sessionExpiresAt;
    private bool _expiryModalShown;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Certificado? _selectedCertificado;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _searchTerm = string.Empty;
    [ObservableProperty] private bool _onlyActive = true;
    [ObservableProperty] private bool _showCnpj = true;
    [ObservableProperty] private bool _hasActiveSession;
    [ObservableProperty] private string _activeSessionId = string.Empty;
    [ObservableProperty] private string _activeSessionThumbprint = string.Empty;
    [ObservableProperty] private string _activeSessionInfo = string.Empty;
    [ObservableProperty] private string _countdownText = "00:00";
    [ObservableProperty] private bool _showExpiryWarning;

    public ObservableCollection<Certificado> Certificados { get; } = [];
    public ObservableCollection<Certificado> FilteredCertificados { get; } = [];

    private const int WARNING_THRESHOLD = 300; // 5 minutes in seconds
    private const int HEARTBEAT_INTERVAL = 30000; // 30 seconds

    public CertificatesViewModel(
        ICertificateService certificateService,
        IDeviceService deviceService,
        IDomainPolicyService domainPolicy,
        ISessionService sessionService,
        ICertificateStoreService certStore)
    {
        _certificateService = certificateService;
        _deviceService = deviceService;
        _domainPolicy = domainPolicy;
        _sessionService = sessionService;
        _certStore = certStore;

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += CountdownTimer_Tick;

        _heartbeatTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(HEARTBEAT_INTERVAL)
        };
        _heartbeatTimer.Tick += HeartbeatTimer_Tick;
    }

    partial void OnSearchTermChanged(string value) => ApplyFilters();
    partial void OnOnlyActiveChanged(bool value) => ApplyFilters();

    [RelayCommand]
    private async Task LoadCertificatesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var certs = await _certificateService.ListAsync();
            Certificados.Clear();
            foreach (var cert in certs)
                Certificados.Add(cert);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ActivateAsync(Certificado certificado)
    {
        if (HasActiveSession)
        {
            ErrorMessage = "Já existe uma sessão ativa. Desative primeiro.";
            return;
        }

        if (certificado.RequiresJustification)
        {
            var dialog = new Views.JustificativaDialog();
            if (dialog.ShowDialog() != true)
                return;

            await DoActivateAsync(certificado, dialog.Justification);
        }
        else
        {
            await DoActivateAsync(certificado, null);
        }
    }

    private async Task DoActivateAsync(Certificado certificado, string? justification)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // 1. Register device
            var hostname = Environment.MachineName;
            var fingerprint = Devices.DeviceService.GenerateFingerprint(hostname);
            var device = await _deviceService.RegisterAsync(
                new Core.DTOs.RegisterDeviceRequest(
                    hostname,
                    GetLocalIpAddress(),
                    Environment.OSVersion.Platform.ToString(),
                    fingerprint,
                    ""));

            // 2. Activate session
            var session = await _certificateService.ActivateAsync(
                certificado.Id,
                device.Id,
                justification);

            // 3. Install PFX to Windows Certificate Store
            if (!string.IsNullOrEmpty(session.PfxBase64) && !string.IsNullOrEmpty(session.PfxPassword))
            {
                var pfxBytes = Convert.FromBase64String(session.PfxBase64);
                var thumbprint = await _certStore.InstallPfxAsync(pfxBytes, session.PfxPassword);
                _activeSessionThumbprint = thumbprint;
            }

            // 4. Update state
            HasActiveSession = true;
            ActiveSessionId = session.SessionId;
            ActiveSessionInfo = $"Sessão #{session.SessionId} • {certificado.Apelido}";
            _sessionExpiresAt = session.ExpiresAt;
            _expiryModalShown = false;

            // 5. Start timers
            UpdateCountdown();
            _countdownTimer.Start();
            _heartbeatTimer.Start();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        if (string.IsNullOrEmpty(ActiveSessionId))
            return;

        IsLoading = true;
        try
        {
            // 1. Stop timers
            _countdownTimer.Stop();
            _heartbeatTimer.Stop();

            // 2. Remove certificate from store
            if (!string.IsNullOrEmpty(_activeSessionThumbprint))
            {
                await _certStore.RemoveByThumbprintAsync(_activeSessionThumbprint);
            }

            // 3. Deactivate session via API
            await _sessionService.DeactivateAsync(ActiveSessionId);

            // 4. Reset state
            HasActiveSession = false;
            ActiveSessionId = string.Empty;
            ActiveSessionThumbprint = string.Empty;
            ActiveSessionInfo = string.Empty;
            CountdownText = "00:00";
            ShowExpiryWarning = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        UpdateCountdown();
    }

    private async void HeartbeatTimer_Tick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(ActiveSessionId))
            return;

        try
        {
            var response = await _sessionService.HeartbeatAsync(ActiveSessionId);

            if (response.Status == "active" && response.ExpiresAt.HasValue)
            {
                // Update expiry time from server
                _sessionExpiresAt = response.ExpiresAt.Value;
                _expiryModalShown = false; // Allow modal to show again if needed
            }
            else if (response.Status is "expired" or "revoked")
            {
                // Session expired/revoked, cleanup
                await DeactivateAsync();
            }
        }
        catch
        {
            // Heartbeat failed, will retry on next tick
        }
    }

    private void UpdateCountdown()
    {
        var remaining = _sessionExpiresAt - DateTime.UtcNow;

        if (remaining.TotalSeconds <= 0)
        {
            _countdownTimer.Stop();
            _heartbeatTimer.Stop();
            _ = DeactivateAsync();
            return;
        }

        var minutes = (int)remaining.TotalMinutes;
        var seconds = remaining.Seconds;
        CountdownText = $"{minutes:D2}:{seconds:D2}";

        // Show warning when less than 5 minutes remaining
        var totalSeconds = (int)remaining.TotalSeconds;
        ShowExpiryWarning = totalSeconds <= WARNING_THRESHOLD;

        // Show expiry modal when less than 5 minutes remaining (only once)
        if (totalSeconds <= WARNING_THRESHOLD && !_expiryModalShown)
        {
            _expiryModalShown = true;
            ShowExpiryModal();
        }
    }

    private void ShowExpiryModal()
    {
        var dialog = new Views.ExpiryDialog(_sessionService, ActiveSessionId, _sessionExpiresAt);

        dialog.Closed += (s, e) =>
        {
            if (dialog.WasRenewed)
            {
                // Countdown will update automatically from the next heartbeat
                _expiryModalShown = false;
            }
            else
            {
                // User closed without renewing, deactivate
                _ = DeactivateAsync();
            }
        };

        dialog.Show();
    }

    private void ApplyFilters()
    {
        FilteredCertificados.Clear();
        foreach (var cert in Certificados)
        {
            if (OnlyActive && cert.Status != "vigente")
                continue;

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.ToLower();
                var name = (cert.Apelido ?? cert.Empresa ?? "").ToLower();
                var cnpj = (cert.Cnpj ?? "").ToLower();
                if (!name.Contains(term) && !cnpj.Contains(term))
                    continue;
            }

            FilteredCertificados.Add(cert);
        }
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Where(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(ua => ua.Address.ToString())
                .FirstOrDefault() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
