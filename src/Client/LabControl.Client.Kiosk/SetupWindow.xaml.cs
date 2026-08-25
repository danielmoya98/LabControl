using System.Windows;
using LabControl.Client.Kiosk.Services;

namespace LabControl.Client.Kiosk;

public partial class SetupWindow : Window
{
    private string _hostname = "";
    private string _ip = "";
    private string _mac = "";
    private List<AulaInfoDto> _aulas = [];

    public SetupWindow()
    {
        InitializeComponent();
        CargarEspecificacionesHardware();
    }

    private void CargarEspecificacionesHardware()
    {
        _hostname = SystemInfoService.GetHostname();
        _ip = SystemInfoService.GetLocalIpAddress();
        _mac = SystemInfoService.GetMacAddress();

        TxtHostname.Text = $"Hostname: {_hostname}";
        TxtIp.Text = $"IP Local: {_ip}";
        TxtMac.Text = $"Dirección MAC: {_mac}";
    }

    private async void OnCargarAulasClick(object sender, RoutedEventArgs e)
    {
        TxtStatus.Foreground = System.Windows.Media.Brushes.DodgerBlue;
        TxtStatus.Text = "⏳ Conectando con la API Central...";

        var apiService = new KioskApiService(TxtApiUrl.Text.Trim());
        _aulas = await apiService.GetAulasDisponiblesAsync();

        if (_aulas.Any())
        {
            CmbAulas.ItemsSource = _aulas.Select(a => $"{a.Nombre} ({a.Pabellon ?? "Sin Pabellón"}) - Capacidad: {a.Capacidad} PCs");
            CmbAulas.SelectedIndex = 0;
            TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            TxtStatus.Text = $"✅ ¡Conectado! Se encontraron {_aulas.Count} aulas en la base de datos.";
        }
        else
        {
            TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            TxtStatus.Text = "⚠️ No se pudieron obtener aulas o no hay aulas registradas en la API. Asegúrate de iniciar la API primero.";
        }
    }

    private async void OnAutoRegistrarClick(object sender, RoutedEventArgs e)
    {
        if (!_aulas.Any() || CmbAulas.SelectedIndex < 0)
        {
            TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            TxtStatus.Text = "⚠️ Por favor haz clic primero en 'Conectar y Leer Aulas' y selecciona un aula válida.";
            return;
        }

        var aulaSeleccionada = _aulas[CmbAulas.SelectedIndex];

        TxtStatus.Foreground = System.Windows.Media.Brushes.DodgerBlue;
        TxtStatus.Text = "⏳ Enviando auto-registro a la API PostgreSQL...";

        var apiService = new KioskApiService(TxtApiUrl.Text.Trim());
        var respuesta = await apiService.AutoRegistrarAsync(aulaSeleccionada.Id, _hostname, _ip, _mac);

        if (respuesta != null)
        {
            // Guardar configuración localmente
            LocalStorageService.SaveConfig(new ConfigModel
            {
                ApiBaseUrl = TxtApiUrl.Text.Trim(),
                AulaId = respuesta.AulaId,
                AulaNombre = aulaSeleccionada.Nombre,
                ComputadoraId = respuesta.ComputadoraId,
                Hostname = respuesta.Hostname,
                MacAddress = respuesta.MacAddress
            });

            MessageBox.Show($"¡Computadora {respuesta.Hostname} vinculada con éxito al {aulaSeleccionada.Nombre}!",
                "Auto-Registro Completo", MessageBoxButton.OK, MessageBoxImage.Information);

            // Abrir la ventana principal del Kiosk
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        else
        {
            TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            TxtStatus.Text = "❌ Error al auto-registrar la computadora. Revisa los logs de la API.";
        }
    }
}
