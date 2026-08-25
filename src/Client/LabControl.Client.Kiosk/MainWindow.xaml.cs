using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using LabControl.Client.Kiosk.Services;

namespace LabControl.Client.Kiosk;

public partial class MainWindow : Window
{
    private ConfigModel? _config;
    private HubConnection? _hubConnection;

    public MainWindow()
    {
        InitializeComponent();
        InicializarKiosk();
    }

    private async void InicializarKiosk()
    {
        _config = LocalStorageService.LoadConfig();

        if (_config == null)
        {
            // Redirigir a configuración si no existe
            var setupWindow = new SetupWindow();
            setupWindow.Show();
            this.Close();
            return;
        }

        TxtAulaInfo.Text = $"{_config.AulaNombre} | Equipo: {_config.Hostname}";
        TxtIpMac.Text = $"IP: {SystemInfoService.GetLocalIpAddress()} | MAC: {_config.MacAddress}";

        await ConectarSignalRAsync();
    }

    private async Task ConectarSignalRAsync()
    {
        if (_config == null) return;

        try
        {
            var hubUrl = new Uri(new Uri(_config.ApiBaseUrl), "hubs/laboratorio").ToString();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>("CierreForzadoRecibido", (hostname) =>
            {
                if (hostname == _config.Hostname)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Sesión finalizada remotamente por el administrador.", "Cierre Forzado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtAlert.Text = "La sesión ha sido finalizada por el administrador.";
                        AlertBorder.Visibility = Visibility.Visible;
                    });
                }
            });

            await _hubConnection.StartAsync();
            TxtSignalRStatus.Text = "● SignalR Conectado en Vivo";
            TxtSignalRStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
        }
        catch
        {
            TxtSignalRStatus.Text = "○ Desconectado (Modo Offline)";
            TxtSignalRStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
        }
    }

    private void OnIniciarSesionClick(object sender, RoutedEventArgs e)
    {
        var email = TxtEmail.Text.Trim();

        // Validar expresión regular de correo institucional @est.univalle.edu
        var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@est\.univalle\.edu$");

        if (!regex.IsMatch(email))
        {
            TxtAlert.Text = "⚠️ Formato inválido. Debe ingresar su correo institucional (@est.univalle.edu).";
            AlertBorder.Visibility = Visibility.Visible;
            return;
        }

        AlertBorder.Visibility = Visibility.Collapsed;
        MessageBox.Show($"¡Bienvenido {email}!\n\nLa terminal ha sido desbloqueada. Tiempo límite: 90 minutos.",
            "Acceso Concedido", MessageBoxButton.OK, MessageBoxImage.Information);

        // Minimizar bloqueo o simular desbloqueo de sesión
    }
}
