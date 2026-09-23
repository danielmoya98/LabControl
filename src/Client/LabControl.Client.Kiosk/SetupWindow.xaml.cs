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

        var existingConfig = LocalStorageService.LoadConfig();
        if (existingConfig != null && !string.IsNullOrWhiteSpace(existingConfig.ApiBaseUrl))
        {
            TxtApiUrl.Text = existingConfig.ApiBaseUrl;
        }

        CargarEspecificacionesHardware();
        Loaded += (s, e) => OnCargarAulasClick(this, new RoutedEventArgs());
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

    private void OnTxtApiUrlKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            OnCargarAulasClick(this, new RoutedEventArgs());
        }
    }

    private void OnCerrarClick(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show("¿Desea cerrar el asistente de configuración?", "Salir", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }

    private async void OnCargarAulasClick(object sender, RoutedEventArgs e)
    {
        var url = TxtApiUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            TxtStatus.Text = "⚠️ Ingrese una URL válida para el servidor central.";
            return;
        }

        TxtStatus.Foreground = System.Windows.Media.Brushes.DodgerBlue;
        TxtStatus.Text = $"⏳ Conectando con {url}...";

        var apiService = new KioskApiService(url);
        _aulas = await apiService.GetAulasDisponiblesAsync();

        if (_aulas.Any())
        {
            CmbAulas.ItemsSource = _aulas.Select(a => $"{a.Nombre} ({a.Pabellon ?? "Sin Pabellón"}) — Capacidad: {a.Capacidad} PCs");
            CmbAulas.SelectedIndex = 0;
            TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            TxtStatus.Text = $"✅ ¡Conexión exitosa! Se encontraron {_aulas.Count} aulas en la base de datos.";
        }
        else
        {
            TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            TxtStatus.Text = $"⚠️ No se pudo conectar a '{url}' o no hay aulas registradas en la base de datos. Verifique que la API central esté en ejecución y la IP sea correcta.";
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
        TxtStatus.Text = $"⏳ Enviando auto-registro al aula '{aulaSeleccionada.Nombre}'...";

        var apiService = new KioskApiService(TxtApiUrl.Text.Trim());
        var hw = SystemInfoService.GetHardwareInfo();
        var respuesta = await apiService.AutoRegistrarAsync(aulaSeleccionada.Id, _hostname, _ip, _mac, hw);

        if (respuesta != null)
        {
            var existingConfig = LocalStorageService.LoadConfig();

            // Guardar configuración localmente
            LocalStorageService.SaveConfig(new ConfigModel
            {
                ApiBaseUrl = TxtApiUrl.Text.Trim(),
                AulaId = respuesta.AulaId,
                AulaNombre = aulaSeleccionada.Nombre,
                ComputadoraId = respuesta.ComputadoraId,
                Hostname = respuesta.Hostname,
                MacAddress = respuesta.MacAddress,
                ClaveTecnico = existingConfig?.ClaveTecnico ?? "AdminLab@2026",
                MinutosInactividadMaximo = respuesta.MinutosInactividadMaximo,
                AccionInactividad = respuesta.AccionInactividad
            });

            MessageBox.Show($"¡Computadora {respuesta.Hostname} vinculada con éxito al {aulaSeleccionada.Nombre}!\n\nDirección MAC: {respuesta.MacAddress}\nIP: {_ip}",
                "Auto-Registro Completo", MessageBoxButton.OK, MessageBoxImage.Information);

            // Abrir la ventana principal del Kiosk
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        else
        {
            TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            TxtStatus.Text = "❌ Error al auto-registrar la computadora. Verifique la conectividad con la API central.";
        }
    }
}
