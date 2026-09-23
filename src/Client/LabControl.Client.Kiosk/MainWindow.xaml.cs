using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.AspNetCore.SignalR.Client;
using LabControl.Client.Kiosk.Services;
using LabControl.Domain.Enums;

namespace LabControl.Client.Kiosk;

public partial class MainWindow : Window
{
    private ConfigModel? _config;
    private HubConnection? _hubConnection;
    private KioskApiService? _apiService;
    private DispatcherTimer? _sessionDurationTimer;
    private int? _sesionActualId;
    private int? _sesionOfflineId;
    private DateTime? _fechaInicioSesion;
    private string? _emailSesionActual;
    private DispatcherTimer? _heartbeatTimer;
    private OfflineSyncWorker? _offlineSyncWorker;
    private readonly List<SecondaryMonitorBlockerWindow> _secondaryBlockers = new();
    private readonly InactivityMonitorService _inactivityService = new();
    private DispatcherTimer? _screenCaptureTimer;

    public MainWindow()
    {
        InitializeComponent();
        InicializarKiosk();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Instalar bloqueo a bajo nivel (Alt+Tab, WinKey, Ctrl+Esc, Alt+F4)
            WindowsHookManager.InstallHook();
            TaskManagerHelper.DisableTaskManager();

            // Bloquear todas las pantallas secundarias conectadas al equipo
            BloquearPantallasSecundarias();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }
        catch
        {
            // Silencioso
        }
    }

    private void OnWindowClosed(object sender, EventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        CerrarPantallasSecundarias();

        // Garantizar liberación de recursos del sistema al cerrar
        WindowsHookManager.UninstallHook();
        TaskManagerHelper.EnableTaskManager();
        _screenCaptureTimer?.Stop();
        _sessionDurationTimer?.Stop();
        _inactivityService.Stop();
        _offlineSyncWorker?.Dispose();
    }

    private void BloquearPantallasSecundarias()
    {
        CerrarPantallasSecundarias();

        try
        {
            var monitors = DisplayMonitorHelper.GetAllMonitors();
            foreach (var mon in monitors)
            {
                if (mon.IsPrimary) continue;

                var blocker = new SecondaryMonitorBlockerWindow(mon, this);
                blocker.Show();
                _secondaryBlockers.Add(blocker);
            }
        }
        catch
        {
            // Silencioso
        }
    }

    private void CerrarPantallasSecundarias()
    {
        foreach (var blocker in _secondaryBlockers)
        {
            try { blocker.Close(); } catch { }
        }
        _secondaryBlockers.Clear();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (this.IsVisible)
            {
                BloquearPantallasSecundarias();
            }
        });
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
        try
        {
            // Inicializar base de datos SQLite local para tolerancia offline
            await SqliteOfflineService.InicializarBaseDeDatosAsync();

            _config = LocalStorageService.LoadConfig();

            if (_config == null)
            {
                // Auto-configuración transparente y silenciosa apuntando a la IP central por defecto
                _config = new ConfigModel
                {
                    ApiBaseUrl = "http://192.168.50.132:5256/",
                    AulaId = 1,
                    AulaNombre = "Laboratorio Central",
                    Hostname = SystemInfoService.GetHostname(),
                    MacAddress = SystemInfoService.GetMacAddress()
                };
                LocalStorageService.SaveConfig(_config);
            }

            _config.ApiBaseUrl = KioskApiService.NormalizeApiUrl(_config.ApiBaseUrl);

            // Siempre garantizar que la dirección MAC y Hostname correspondan al hardware físico real
            var realMac = SystemInfoService.GetMacAddress();
            var realHostname = SystemInfoService.GetHostname();
            bool necesitaGuardar = false;

            if (string.IsNullOrWhiteSpace(_config.MacAddress) || _config.MacAddress == "00:00:00:00:00:00" || _config.MacAddress != realMac)
            {
                _config.MacAddress = realMac;
                necesitaGuardar = true;
            }

            if (string.IsNullOrWhiteSpace(_config.Hostname) || _config.Hostname != realHostname)
            {
                _config.Hostname = realHostname;
                necesitaGuardar = true;
            }

            if (necesitaGuardar)
            {
                LocalStorageService.SaveConfig(_config);
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

            // Auditar y reportar a la API si la computadora sufrió un corte previo de energía o apagado de fuerza bruta
            _ = WindowsEventLogService.VerificarYReportarApagadoForzadoAsync(_apiService, _config);

            await ConectarSignalRAsync();

            IniciarHeartbeatTimer();
            IniciarScreenCaptureTimer();
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                TxtSignalRStatus.Text = "○ Modo Offline (Servidor no alcanzable)";
                TxtSignalRStatus.Foreground = System.Windows.Media.Brushes.IndianRed;
            });

            try
            {
                var logPath = System.IO.Path.Combine(LocalStorageService.GetDataDirectory(), "kiosk-crash.log");
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [InicializarKiosk] {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { }
        }
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
                _config.MinutosInactividadMaximo = respuesta.MinutosInactividadMaximo;
                _config.AccionInactividad = respuesta.AccionInactividad;
                LocalStorageService.SaveConfig(_config);
            }
        }
        catch
        {
            // Si la API no está disponible en este instante, continuará en modo local
        }
    }

    private void IniciarScreenCaptureTimer()
    {
        _screenCaptureTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(4)
        };
        _screenCaptureTimer.Tick += async (s, e) =>
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected && _config != null)
            {
                try
                {
                    var bytes = ScreenCaptureService.CapturarPantallaMiniatura();
                    if (bytes != null && bytes.Length > 0)
                    {
                        var base64 = Convert.ToBase64String(bytes);
                        await _hubConnection.InvokeAsync("EnviarMiniaturaPantalla", _config.Hostname, base64);
                    }
                }
                catch
                {
                    // Silencioso
                }
            }
        };
        _screenCaptureTimer.Start();
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
            _hubConnection.On<object>("RecibirComandoCierreSesion", (motivoObj) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    int motivo = (int)TipoCierreSesion.AdminRemoto;
                    try
                    {
                        if (motivoObj is System.Text.Json.JsonElement je)
                        {
                            if (je.ValueKind == System.Text.Json.JsonValueKind.Number && je.TryGetInt32(out int n))
                                motivo = n;
                            else if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                if (Enum.TryParse<TipoCierreSesion>(je.GetString(), true, out var pEnum))
                                    motivo = (int)pEnum;
                                else if (int.TryParse(je.GetString(), out int pInt))
                                    motivo = pInt;
                            }
                        }
                        else if (motivoObj is int i) motivo = i;
                        else if (motivoObj is string s && Enum.TryParse<TipoCierreSesion>(s, true, out var parsed))
                            motivo = (int)parsed;
                    }
                    catch { }

                    await ProcesarCierreSesionRemotoAsync(motivo);
                });
            });

            // Escuchar alertas y mensajes transmitidos desde el WebAdmin (Banner flotante no intrusivo)
            _hubConnection.On<string>("RecibirAlertaTerminal", (mensaje) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var toast = new NotificationToastWindow(mensaje);
                    toast.Show();
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

        // Validar expresión regular de correo institucional (@est.univalle.edu o @univalle.edu)
        var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@(est\.univalle\.edu|univalle\.edu)$", RegexOptions.IgnoreCase);

        if (!regex.IsMatch(email))
        {
            TxtAlert.Text = "⚠️ Formato inválido. Ingrese su correo institucional (@est.univalle.edu o @univalle.edu).";
            AlertBorder.Visibility = Visibility.Visible;
            return;
        }

        AlertBorder.Visibility = Visibility.Collapsed;

        if (_apiService == null || _config == null)
        {
            _apiService = new KioskApiService(_config?.ApiBaseUrl ?? "http://192.168.50.132:5256/");
        }

        _fechaInicioSesion = DateTime.UtcNow;
        _emailSesionActual = email;

        // Intentar registrar el inicio de sesión en PostgreSQL vía API central
        IniciarSesionResult res;
        try
        {
            res = await _apiService.IniciarSesionAsync(_config?.ComputadoraId, _config?.Hostname, email, 90);
        }
        catch
        {
            res = new IniciarSesionResult { Exito = false, EsErrorConexion = true };
        }

        if (res.Exito && res.Datos != null)
        {
            var datos = res.Datos;

            // ===== CASO ONLINE =====
            _sesionActualId = datos.SesionId;
            _sesionOfflineId = null;

            if (datos.ComputadoraId > 0 && _config != null)
            {
                _config.ComputadoraId = datos.ComputadoraId;
            }

            // Persistir usuario activo en configuración local para certificar posibles cortes abruptos de energía
            if (_config != null)
            {
                _config.UltimoEstudianteSesion = email;
                LocalStorageService.SaveConfig(_config);
            }

            // Liberar hooks del teclado para permitir uso de la PC, pero mantener Task Manager deshabilitado contra sabotajes
            WindowsHookManager.UninstallHook();
            TaskManagerHelper.DisableTaskManager();
            CerrarPantallasSecundarias();
            this.Hide();

            // Iniciar timer de duración de sesión en segundo plano (sin widget visual invasivo)
            IniciarTimerDuracionSesion(datos.MinutosLimite);

            // Iniciar monitoreo de inactividad física
            _inactivityService.Start(
                _config?.MinutosInactividadMaximo ?? 15,
                (_config?.AccionInactividad ?? 0) == 0,
                (esApagar) =>
                {
                    Dispatcher.Invoke(async () =>
                    {
                        await ProcesarInactividadAsync(esApagar);
                    });
                }
            );
        }
        else if (!res.EsErrorConexion && !string.IsNullOrWhiteSpace(res.MensajeError))
        {
            // Rechazado por reglas institucionales de horario (ej. receso o clase por comenzar)
            TxtAlert.Text = $"⚠️ {res.MensajeError}";
            AlertBorder.Visibility = Visibility.Visible;
            return;
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

                    // Liberar hooks del teclado para permitir uso de la PC, pero mantener Task Manager deshabilitado contra sabotajes
                    WindowsHookManager.UninstallHook();
                    TaskManagerHelper.DisableTaskManager();
                    CerrarPantallasSecundarias();
                    this.Hide();

                    // Iniciar timer con límite por defecto de 90 minutos en segundo plano (sin widget visual)
                    IniciarTimerDuracionSesion(90);

                    _inactivityService.Start(
                        _config?.MinutosInactividadMaximo ?? 15,
                        (_config?.AccionInactividad ?? 0) == 0,
                        (esApagar) =>
                        {
                            Dispatcher.Invoke(async () =>
                            {
                                await ProcesarInactividadAsync(esApagar);
                            });
                        }
                    );
                }
                catch (Exception)
                {
                    TxtAlert.Text = "⚠️ Error temporal al validar credenciales locales. Contacte al encargado.";
                    AlertBorder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                TxtAlert.Text = "⚠️ Servicio de laboratorio no disponible en este momento. Intente en unos minutos.";
                AlertBorder.Visibility = Visibility.Visible;
            }
        }
    }

    public void FinalizarSesionPorApagadoSistema()
    {
        if (_sesionActualId.HasValue || _sesionOfflineId.HasValue)
        {
            try
            {
                FinalizarSesionInternoAsync((int)TipoCierreSesion.ApagadoForzado).Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
        }
    }

    private void IniciarTimerDuracionSesion(int minutos)
    {
        _sessionDurationTimer?.Stop();
        int duracion = minutos > 0 ? minutos : 90;
        _sessionDurationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(duracion)
        };
        _sessionDurationTimer.Tick += (s, e) =>
        {
            _sessionDurationTimer.Stop();
            OnSesionTerminada((int)TipoCierreSesion.FinPeriodo);
        };
        _sessionDurationTimer.Start();
    }

    private async Task ProcesarInactividadAsync(bool esApagar)
    {
        _inactivityService.Stop();
        _sessionDurationTimer?.Stop();

        // 1. Notificar y persistir fin de sesión con código 6 (TipoCierreSesion.Inactividad)
        // Se aguarda el registro antes de ordenar el apagado para garantizar recepción por el backend
        await FinalizarSesionInternoAsync((int)TipoCierreSesion.Inactividad);

        if (esApagar)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/s /t 5 /c \"Apagado automatico por inactividad fisica prolongada en laboratorio\" /f",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch { }
        }
        else
        {
            RestaurarBloqueoKiosk("⚠️ Sesión cerrada automáticamente por inactividad física prolongada.");
        }
    }

    private async Task ProcesarCierreSesionRemotoAsync(int motivo)
    {
        _inactivityService.Stop();
        _sessionDurationTimer?.Stop();

        await FinalizarSesionInternoAsync(motivo);

        RestaurarBloqueoKiosk("⚠️ Sesión finalizada remotamente por el administrador del laboratorio.");
    }

    private async void OnSesionTerminada(int tipoCierre)
    {
        _inactivityService.Stop();
        _sessionDurationTimer?.Stop();

        await FinalizarSesionInternoAsync(tipoCierre);

        string mensaje = tipoCierre switch
        {
            (int)TipoCierreSesion.Inactividad => "⚠️ Sesión cerrada automáticamente por inactividad física prolongada.",
            (int)TipoCierreSesion.FinPeriodo => "ℹ️ El tiempo asignado a su sesión o clase ha finalizado.",
            (int)TipoCierreSesion.AdminRemoto => "⚠️ Sesión finalizada remotamente por el administrador.",
            _ => "ℹ️ La sesión ha concluido. Terminal bloqueada."
        };

        RestaurarBloqueoKiosk(mensaje);
    }

    private async Task FinalizarSesionInternoAsync(int tipoCierre)
    {
        var fechaFin = DateTime.UtcNow;

        // 1. Si la sesión inició en modo offline:
        if (_sesionOfflineId.HasValue)
        {
            try
            {
                await SqliteOfflineService.RegistrarFinSesionOfflineAsync(_sesionOfflineId.Value, fechaFin, tipoCierre);
                _ = _offlineSyncWorker?.SincronizarPendientesAsync();
            }
            catch { }
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
                catch { }
            }

            _sesionActualId = null;
        }

        _emailSesionActual = null;
        _fechaInicioSesion = null;

        if (_config != null)
        {
            _config.UltimoEstudianteSesion = null;
            LocalStorageService.SaveConfig(_config);
        }
    }

    private void RestaurarBloqueoKiosk(string? mensajeAlerta)
    {
        Dispatcher.Invoke(() =>
        {
            this.Show();
            this.WindowState = WindowState.Maximized;
            this.Topmost = true;
            this.Activate();

            WindowsHookManager.InstallHook();
            TaskManagerHelper.DisableTaskManager();
            BloquearPantallasSecundarias();

            TxtEmail.Text = "";

            if (!string.IsNullOrWhiteSpace(mensajeAlerta))
            {
                TxtAlert.Text = mensajeAlerta;
                AlertBorder.Visibility = Visibility.Visible;
            }
            else
            {
                AlertBorder.Visibility = Visibility.Collapsed;
            }
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
            if (_sesionActualId.HasValue || _sesionOfflineId.HasValue)
            {
                try
                {
                    FinalizarSesionInternoAsync((int)TipoCierreSesion.ApagadoForzado).Wait(TimeSpan.FromSeconds(2));
                }
                catch { }
            }

            var cmd = tipoComando?.Trim().ToUpperInvariant() == "RESTART"
                ? "/r /t 2 /f"
                : "/s /t 2 /f";

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
