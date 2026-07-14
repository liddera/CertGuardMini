using System.Windows;
using System.Windows.Threading;
using CertGuard.Core.Interfaces;

namespace CertGuard.Desktop.Views;

public partial class ExpiryDialog : Window
{
    private readonly ISessionService _sessionService;
    private readonly string _sessionId;
    private readonly DispatcherTimer _countdownTimer;
    private DateTime _expiresAt;
    private int _remainingSeconds;

    public bool WasRenewed { get; private set; }

    public ExpiryDialog(ISessionService sessionService, string sessionId, DateTime expiresAt)
    {
        InitializeComponent();
        _sessionService = sessionService;
        _sessionId = sessionId;
        _expiresAt = expiresAt;

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += CountdownTimer_Tick;

        UpdateCountdown();
        _countdownTimer.Start();
    }

    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        _remainingSeconds = (int)(_expiresAt - DateTime.UtcNow).TotalSeconds;

        if (_remainingSeconds <= 0)
        {
            _countdownTimer.Stop();
            DialogResult = false;
            Close();
            return;
        }

        var minutes = _remainingSeconds / 60;
        var seconds = _remainingSeconds % 60;
        CountdownText.Text = $"{minutes:D2}:{seconds:D2}";
        SecondsText.Text = $"(ou {_remainingSeconds} segundos)";
    }

    private async void Renew_Click(object sender, RoutedEventArgs e)
    {
        RenewButton.IsEnabled = false;
        RenewButton.Content = "Renovando...";

        try
        {
            var response = await _sessionService.HeartbeatAsync(_sessionId);
            if (response.Status == "active" && response.ExpiresAt.HasValue)
            {
                _expiresAt = response.ExpiresAt.Value;
                WasRenewed = true;
                _countdownTimer.Stop();
                DialogResult = true;
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao renovar: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RenewButton.IsEnabled = true;
            RenewButton.Content = "✓ Renovar sessão";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _countdownTimer.Stop();
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _countdownTimer.Stop();
        base.OnClosed(e);
    }
}
