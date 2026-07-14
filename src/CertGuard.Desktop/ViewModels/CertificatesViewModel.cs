using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
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

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Certificado? _selectedCertificado;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _searchTerm = string.Empty;
    [ObservableProperty] private bool _onlyActive = true;
    [ObservableProperty] private bool _showCnpj = true;
    [ObservableProperty] private bool _hasActiveSession;
    [ObservableProperty] private string _activeSessionId = string.Empty;
    [ObservableProperty] private string _activeSessionInfo = string.Empty;
    [ObservableProperty] private string _countdownText = string.Empty;
    [ObservableProperty] private bool _showExpiryWarning;

    public ObservableCollection<Certificado> Certificados { get; } = [];
    public ObservableCollection<Certificado> FilteredCertificados { get; } = [];

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
            var hostname = Environment.MachineName;
            var fingerprint = Devices.DeviceService.GenerateFingerprint(hostname);
            var device = await _deviceService.RegisterAsync(
                new Core.DTOs.RegisterDeviceRequest(
                    hostname,
                    System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                        .FirstOrDefault()?.GetIPProperties().UnicastAddresses
                        .FirstOrDefault()?.Address?.ToString() ?? "127.0.0.1",
                    Environment.OSVersion.Platform.ToString(),
                    fingerprint,
                    ""));

            var session = await _certificateService.ActivateAsync(
                certificado.Id,
                device.Id,
                justification);

            HasActiveSession = true;
            ActiveSessionId = session.SessionId;
            ActiveSessionInfo = $"Sessão #{session.SessionId} • {certificado.Apelido}";
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
            await _sessionService.DeactivateAsync(ActiveSessionId);
            HasActiveSession = false;
            ActiveSessionId = string.Empty;
            ActiveSessionInfo = string.Empty;
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

    public static string GetStatusColor(string status) => status switch
    {
        "vigente" => "#10b981",
        "vencido" => "#ef4444",
        "bloqueado" => "#ef4444",
        "inativo" => "#6b7280",
        _ => "#6b7280"
    };

    public static string GetStatusBgColor(string status) => status switch
    {
        "vigente" => "#d1fae5",
        "vencido" => "#fee2e2",
        "bloqueado" => "#fee2e2",
        "inativo" => "#f3f4f6",
        _ => "#f3f4f6"
    };
}
