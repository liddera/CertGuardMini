using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CertGuard.Core.Interfaces;
using CertGuard.Services.Auth;
using CertGuard.Services.Crypto;
using CertGuard.Services.Devices;
using CertGuard.Services.Certificates;
using CertGuard.Services.Sessions;
using CertGuard.Services.Proxy;
using CertGuard.Services.Audit;
using CertGuard.Services.Http;
using CertGuard.Desktop.ViewModels;
using CertGuard.Desktop.Views;

namespace CertGuard.Desktop;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateApplicationBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddHttpClient("backend", client =>
                {
                    client.BaseAddress = new Uri("https://homolog.lidderaplus.com.br/api");
                });

                services.AddSingleton<ITokenStorage, TokenStorage>();
                services.AddTransient<AuthHandler>();

                services.AddHttpClient<IAuthService, AuthService>("backend");
                services.AddHttpClient<IDeviceService, DeviceService>("backend");
                services.AddHttpClient<ICertificateService, CertificateService>("backend");
                services.AddHttpClient<ISessionService, SessionService>("backend");
                services.AddSingleton<IKeyGenService, KeyGenService>();
                services.AddSingleton<ICertificateStoreService, CertificateStoreService>();
                services.AddSingleton<IDomainPolicyService, DomainPolicyService>();
                services.AddHttpClient<IAuditService, AuditService>("backend");
                services.AddHttpClient<NavigationPolicyService>("backend");

                services.AddHostedService<HeartbeatService>();

                services.AddTransient<AuthViewModel>();
                services.AddTransient<CertificatesViewModel>();
                services.AddTransient<SessionViewModel>();

                services.AddSingleton<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<CertificatesPage>();
                services.AddTransient<ExpiryDialog>();
                services.AddTransient<JustificativaDialog>();
            })
            .Build();

        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : false;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value == null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLower();
        return status switch
        {
            "vigente" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
            "vencido" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            "bloqueado" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            "inativo" => new SolidColorBrush(Color.FromRgb(107, 114, 128)),
            _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToBgColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLower();
        return status switch
        {
            "vigente" => new SolidColorBrush(Color.FromRgb(209, 250, 229)),
            "vencido" => new SolidColorBrush(Color.FromRgb(254, 226, 226)),
            "bloqueado" => new SolidColorBrush(Color.FromRgb(254, 226, 226)),
            "inativo" => new SolidColorBrush(Color.FromRgb(243, 244, 246)),
            _ => new SolidColorBrush(Color.FromRgb(243, 244, 246))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
