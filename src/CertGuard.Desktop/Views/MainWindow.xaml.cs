using System.ComponentModel;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace CertGuard.Desktop.Views;

public partial class MainWindow : Window
{
    private TaskbarIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "CertGuard Desktop",
            DoubleClickCommand = new TrayDoubleClickCommand(this)
        };

        var menu = new System.Windows.Controls.ContextMenu();
        var openItem = new System.Windows.Controls.MenuItem { Header = "Abrir" };
        openItem.Click += (s, e) => Show();
        var exitItem = new System.Windows.Controls.MenuItem { Header = "Sair" };
        exitItem.Click += (s, e) => Application.Current.Shutdown();
        menu.Items.Add(openItem);
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenu = menu;

        MainFrame.Navigate(new CertificatesPage());
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        _trayIcon?.ShowBalloonTip("CertGuard", "O aplicativo continua rodando em segundo plano.", Hardcodet.Wpf.TaskbarNotification.BalloonIconType.Info);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void ShowCertificates_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new CertificatesPage());
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _trayIcon?.Dispose();
        Application.Current.Shutdown();
    }
}

public class TrayDoubleClickCommand : System.Windows.Input.ICommand
{
    private readonly Window _window;
    public TrayDoubleClickCommand(Window window) => _window = window;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _window.Show();
}
