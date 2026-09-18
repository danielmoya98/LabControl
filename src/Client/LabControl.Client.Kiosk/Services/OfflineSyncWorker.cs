using System.Windows.Threading;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Worker en segundo plano para sincronizar automáticamente sesiones almacenadas en SQLite local
/// hacia la API central PostgreSQL cuando se restablece la conectividad de red.
/// </summary>
public class OfflineSyncWorker : IDisposable
{
    private readonly KioskApiService _apiService;
    private readonly ConfigModel _config;
    private readonly DispatcherTimer _timer;
    private bool _estaSincronizando = false;

    public OfflineSyncWorker(KioskApiService apiService, ConfigModel config, int intervaloSegundos = 45)
    {
        _apiService = apiService;
        _config = config;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(intervaloSegundos)
        };
        _timer.Tick += async (s, e) => await SincronizarPendientesAsync();
    }

    public void Start()
    {
        _timer.Start();
        // Ejecutar un intento inmediato de sincronización al iniciar
        Task.Run(SincronizarPendientesAsync);
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public async Task SincronizarPendientesAsync()
    {
        if (_estaSincronizando) return;

        try
        {
            _estaSincronizando = true;

            var pendientes = await SqliteOfflineService.ObtenerSesionesPendientesAsync();
            if (pendientes.Count == 0) return;

            // Verificar si el servidor está respondiendo
            bool hayConexion = await _apiService.PingAsync();
            if (!hayConexion) return;

            var sesionesDto = pendientes.Select(p => new SesionBatchItemDto
            {
                EmailEstudiante = p.EmailEstudiante,
                FechaHoraInicio = p.FechaHoraInicio,
                FechaHoraFin = p.FechaHoraFin,
                TipoCierre = p.TipoCierre
            }).ToList();

            string hostname = !string.IsNullOrWhiteSpace(_config.Hostname) ? _config.Hostname : Environment.MachineName;

            bool exito = await _apiService.SincronizarBatchAsync(hostname, sesionesDto);

            if (exito)
            {
                foreach (var sesion in pendientes)
                {
                    await SqliteOfflineService.MarcarSesionSincronizadaAsync(sesion.Id);
                }
            }
            else
            {
                foreach (var sesion in pendientes)
                {
                    await SqliteOfflineService.IncrementarIntentoAsync(sesion.Id);
                }
            }
        }
        catch
        {
            // Falla de conexión tolerante; se reintentará en el siguiente ciclo
        }
        finally
        {
            _estaSincronizando = false;
        }
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
