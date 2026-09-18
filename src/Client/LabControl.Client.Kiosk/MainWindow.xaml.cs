using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using LabControl.Client.Kiosk.Services;
using LabControl.Domain.Enums;

namespace LabControl.Client.Kiosk;

public partial class MainWindow : Window
{
    private ConfigModel? _config;
    private HubConnection? _hubConnection;
    private KioskApiService? _apiService;
    private SessionWidgetWindow? _sessionWidget;
    private int? _sesionActualId;
    private int? _sesionOfflineId;
    private DateTime? _fechaInicioSesion;
    private string? _emailSesionActual;
    private DispatcherTimer? _heartbeatTimer;
    private OfflineSyncWorker? _offlineSyncWorker;

    public MainWindow()
    {
        InitializeComponent();
        InicializarKiosk();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Instalar bloqueo a bajo nivel (Alt+Tab, WinKey, Ctrl+Esc, Alt+F4)
        WindowsHookManager.InstallHook();
        TaskManagerHelper.DisableTaskManager();
    }

    private void OnWindowClosed(object sender, EventArgs e)
    {
        // Garantizar liberación de recursos del sistema al cerrar
        WindowsHookManager.UninstallHook();
        TaskManagerHelper.EnableTaskManager();
        _offlineSyncWorker?.Dispose();
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Atajo para soporte técnico: Ctrl + Shift + F12
        if (e.Key == Key.F12 &&
            (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            e.Handled = true;
            AbrirDialogoSoporteTecnico();
        }
    }

    private void OnSoporteTecnicoClick(object sender, RoutedEventArgs e)
    {
        AbrirDialogoSoporteTecnico();
    }

    private void AbrirDialogoSoporteTecnico()
    {
        var dialog = new TecnicoUnlockDialog(this, _config?.ClaveTecnico);
        dialog.ShowDialog();
    }

    private async void InicializarKiosk()
    {
        // Inicializar base de datos SQLite local para tolerancia offline
        await SqliteOfflineService.InicializarBaseDeDatosAsync();

        _config = LocalStorageService.LoadConfig();

        if (_config == null)
        {
            // Redirigir a configuración si no existe
            WindowsHookManager.UninstallHook();
            TaskManagerHelper.EnableTaskManager();
            var setupWindow = new SetupWindow();
            setupWindow.Show();
            this.Close();
            return;
        }

        _apiService = new KioskApiService(_config.ApiBaseUrl);

        // Iniciar agente de sincronización offline en segundo plano
        _offlineSyncWorker = new OfflineSyncWorker(_apiService, _config);
        _offlineSyncWorker.Start();

        var currentIp = SystemInfoService.GetLocalIpAddress();
        TxtAulaInfo.Text = $"{_config.AulaNombre} | Equipo: {_config.Hostname}";
        TxtIpMac.Text = $"IP: {currentIp} | MAC: {_config.MacAddress}";

        // Asegurar auto-registro en la base de datos PostgreSQL
        await AsegurarRegistroComputadoraAsync(currentIp);

        await ConectarSignalRAsync();

        IniciarHeartbeatTimer();
    }

    private async Task AsegurarRegistroComputadoraAsync(string currentIp)
    {
        if (_config == null || _apiService == null) return;

        try
        {
            var respuesta = await _apiService.AutoRegistrarAsync(
                _config.AulaId,
                _config.Hostname,
                currentIp,
                _config.MacAddress);

            if (respuesta != null && respuesta.ComputadoraId > 0)
            {
                _config.ComputadoraId = respuesta.ComputadoraId;
                LocalStorageService.SaveConfig(_config);
            }
        }
        catch
        {
            // Si la API no está disponible en este instante, continuará en modo local
        }
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

            _hubConnection.Reconnecting += (error) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtSignalRStatus.Text = "◐ Reconectando Socket...";
                    TxtSignalRStatus.Foreground = System.Windows.Media.Brushes.Orange;
                });
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += (connectionId) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    TxtSignalRStatus.Text = "● SignalR Conectado";
                    TxtSignalRStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    await RegistrarseEnHubAsync();
                    // Al reconectar, disparar sincronización offline
                    _ = _offlineSyncWorker?.SincronizarPendientesAsync();
                });
                return Task.CompletedTask;
            };

            _hubConnection.Closed += (error) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtSignalRStatus.Text = "○ Socket Desconectado (Modo Resiliente)";
                    TxtSignalRStatus.Foreground = System.Windows.Media.Brushes.IndianRed;
                });
                return Task.CompletedTask;
            };

            // Escuchar comando de cierre remoto emitido por el administrador
            _hubConnection.On<int>("RecibirComandoCierreSesion", (motivo) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_sessionWidget != null)
                    {
                        _sessionWidget.CerrarPorComandoRemoto();
                    }
                });
            });

            // Escuchar alertas y mensajes transmitidos desde el WebAdmin
            _hubConnection.On<string>("RecibirAlertaTerminal", (mensaje) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var alerta = new AlertaMensajeDialog(mensaje);
                    alerta.ShowDialog();
                });
            });

            // Escuchar comandos de energía remota (Apagar / Reiniciar) emitidos por WebAdmin
            _hubConnection.On<string, string>("RecibirComandoEnergia", (tipoComando, motivo) =>
            {
                Dispatcher.Invoke(() =>
                {
                    EjecutarComandoEnergia(tipoComando, motivo);
                });
            });

            await _hubConnection.StartAsync();

            Dispatcher.Invoke(() =>
            {
                TxtSignalRStatus.Text = "● SignalR Conectado";
                TxtSignalRStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            });

            await RegistrarseEnHubAsync();
        }
        catch
        {
            Dispatcher.Invoke(() =>
            {
                TxtSignalRStatus.Text = "○ Sin conexión al servidor (Modo Resiliente)";
                TxtSignalRStatus.Foreground = System.Windows.Media.Brushes.IndianRed;
            });
        }
    }

    private async Task RegistrarseEnHubAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected && _config != null)
        {
            try
            {
                await _hubConnection.InvokeAsync("RegistrarTerminal", _config.Hostname, _config.MacAddress, _config.AulaId);
            }
            catch
            {
                // Tolerancia a fallos en registro
            }
        }
    }

    private void IniciarHeartbeatTimer()
    {
        _heartbeatTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(25)
        };
        _heartbeatTimer.Tick += async (s, e) => await EnviarHeartbeatAsync();
        _heartbeatTimer.Start();
    }

    private async Task EnviarHeartbeatAsync()
    {
        if (_config == null) return;

        var currentIp = SystemInfoService.GetLocalIpAddress();

        // 1. Prioridad: enviar heartbeat vía SignalR WebSocket con telemetría en tiempo real
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            try
            {
                var cpuUso = SystemInfoService.GetCpuUsagePercentage();
                var (_, ramUso) = SystemInfoService.GetMemoryMetrics();
                var (_, discoLibre) = SystemInfoService.GetDiskMetrics();

                await _hubConnection.InvokeAsync("EnviarHeartbeatConTelemetria", _config.Hostname, _config.MacAddress, currentIp, cpuUso, ramUso, discoLibre);
                return;
            }
            catch
            {
                // Fallback a HTTP si SignalR falla momentáneamente
            }
        }

        // 2. Fallback: enviar heartbeat vía HTTP REST
        if (_apiService != null)
        {
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
                var payload = new
                {
                    Hostname = _config.Hostname,
                    MacAddress = _config.MacAddress,
                    IpActual = currentIp,
                    AulaId = _config.AulaId
                };
                await client.PostAsJsonAsync("api/control/heartbeat", payload);
            }
            catch
            {
                // Modo offline: silencioso
            }
        }
    }

    private async void OnIniciarSesionClick(object sender, RoutedEventArgs e)
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

        if (_apiService == null || _config == null)
        {
            _apiService = new KioskApiService(_config?.ApiBaseUrl ?? "http://localhost:5256/");
        }

        _fechaInicioSesion = DateTime.UtcNow;
        _emailSesionActual = email;

        // Intentar registrar el inicio de sesión en PostgreSQL vía API central
        IniciarSesionApiResponse? res = null;
        try
        {
            res = await _apiService.IniciarSesionAsync(_config?.ComputadoraId, _config?.Hostname, email, 90);
        }
        catch
        {
            res = null;
        }

        if (res != null)
        {
            // ===== CASO ONLINE =====
            _sesionActualId = res.SesionId;
            _sesionOfflineId = null;

            if (res.ComputadoraId > 0 && _config != null)
            {
                _config.ComputadoraId = res.ComputadoraId;
            }

            // Liberar hooks y ocultar ventana de bloqueo
            WindowsHookManager.UninstallHook();
            TaskManagerHelper.EnableTaskManager();
            this.Hide();

            // Abrir widget flotante con cronómetro en vivo
            _sessionWidget = new SessionWidgetWindow(
                email,
                _config?.AulaNombre ?? "Laboratorio",
                res.MinutosLimite,
                OnSesionTerminada
            );
            _sessionWidget.Show();
        }
        else
        {
            // ===== CASO OFFLINE RESILIENTE =====
            // Si no hay conexión pero la computadora ya fue configurada en el laboratorio,
            // permitimos el acceso registrando la sesión en la base de datos local SQLite.
            if (_config != null && !string.IsNullOrWhiteSpace(_config.Hostname))
            {
                try
                {
                    int offlineId = await SqliteOfflineService.RegistrarInicioSesionOfflineAsync(
                        _config.ComputadoraId,
                        _config.Hostname,
                        email,
                        _fechaInicioSesion.Value
                    );

                    _sesionOfflineId = offlineId;
                    _sesionActualId = null;

                    // Liberar hooks y ocultar pantalla de bloqueo
                    WindowsHookManager.UninstallHook();
                    TaskManagerHelper.EnableTaskManager();
                    this.Hide();

                    // Iniciar widget con límite por defecto de 90 minutos
                    _sessionWidget = new SessionWidgetWindow(
                        $"{email} (Modo Local)",
                        _config.AulaNombre,
                        90,
                        OnSesionTerminada
                    );
                    _sessionWidget.Show();
                }
                catch (Exception ex)
                {
                    TxtAlert.Text = $"❌ Error al iniciar sesión en modo offline: {ex.Message}";
                    AlertBorder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                TxtAlert.Text = "❌ No hay conexión con el servidor y la terminal no está configurada.";
                AlertBorder.Visibility = Visibility.Visible;
            }
        }
    }

    private async void OnSesionTerminada(int tipoCierre)
    {
        var fechaFin = DateTime.UtcNow;

        // 1. Si la sesión inició en modo offline:
        if (_sesionOfflineId.HasValue)
        {
            try
            {
                await SqliteOfflineService.RegistrarFinSesionOfflineAsync(_sesionOfflineId.Value, fechaFin, tipoCierre);
                // Disparar sincronización por si volvió la red
                _ = _offlineSyncWorker?.SincronizarPendientesAsync();
            }
            catch
            {
                // Silencioso
            }
            _sesionOfflineId = null;
        }
        // 2. Si la sesión inició en modo online:
        else if (_sesionActualId.HasValue)
        {
            bool finNotificado = false;
            if (_apiService != null)
            {
                try
                {
                    var resp = await _apiService.FinalizarSesionAsync(_sesionActualId, _config?.ComputadoraId, _config?.Hostname, tipoCierre);
                    finNotificado = resp != null;
                }
                catch
                {
                    finNotificado = false;
                }
            }

            // Si la API no respondió al cerrar (red caída en el transcurso), guardar en cola SQLite
            if (!finNotificado && _config != null && !string.IsNullOrWhiteSpace(_emailSesionActual) && _fechaInicioSesion.HasValue)
            {
                try
                {
                    await SqliteOfflineService.EncolarSesionCompletaOfflineAsync(
                        _config.ComputadoraId,
                        _config.Hostname,
                        _emailSesionActual,
                        _fechaInicioSesion.Value,
                        fechaFin,
                        tipoCierre
                    );
                }
                catch
                {
                    // Silencioso
                }
            }

            _sesionActualId = null;
        }

        _sessionWidget = null;
        _emailSesionActual = null;
        _fechaInicioSesion = null;

        // Restaurar pantalla completa de bloqueo y reinstalar hooks
        Dispatcher.Invoke(() =>
        {
            this.Show();
            this.WindowState = WindowState.Maximized;
            this.Topmost = true;
            this.Activate();

            WindowsHookManager.InstallHook();
            TaskManagerHelper.DisableTaskManager();

            TxtEmail.Text = "";
            TxtAlert.Text = "ℹ️ La sesión ha concluido. Terminal bloqueada.";
            AlertBorder.Visibility = Visibility.Visible;
        });
    }

    private void OnReconfigurarTerminalClick(object sender, RoutedEventArgs e)
    {
        WindowsHookManager.UninstallHook();
        TaskManagerHelper.EnableTaskManager();
        var setupWindow = new SetupWindow();
        setupWindow.Show();
        this.Close();
    }

    private void OnApagarTerminalClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "¿Deseas apagar este equipo físico ahora?\n\nEsta acción apagará el computador sin requerir inicio de sesión.",
            "Confirmar Apagado de Equipo", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            EjecutarComandoEnergia("SHUTDOWN", "Apagado manual desde pantalla de bloqueo");
        }
    }

    private void EjecutarComandoEnergia(string tipoComando, string motivo)
    {
        try
        {
            // Cerrar sesión activa si existiera
            _sessionWidget?.CerrarPorComandoRemoto();

            var cmd = tipoComando?.Trim().ToUpperInvariant() == "RESTART"
                ? "/r /t 0 /f"
                : "/s /t 0 /f";

            var psi = new ProcessStartInfo("shutdown.exe", cmd)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch
        {
            // Fallback
        }
    }
}
