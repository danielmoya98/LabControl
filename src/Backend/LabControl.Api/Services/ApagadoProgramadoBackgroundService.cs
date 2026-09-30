namespace LabControl.Api.Services;

public class ApagadoProgramadoBackgroundService : BackgroundService
{
    private readonly CampusShutdownManager _shutdownManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApagadoProgramadoBackgroundService> _logger;
    private string? _ultimoAvisoEnviadoDia;

    public ApagadoProgramadoBackgroundService(
        CampusShutdownManager shutdownManager,
        IServiceScopeFactory scopeFactory,
        ILogger<ApagadoProgramadoBackgroundService> logger)
    {
        _shutdownManager = shutdownManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Apagado Nocturno Automático del Campus iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var config = _shutdownManager.GetConfiguracion();

                if (config.Habilitado && !string.IsNullOrWhiteSpace(config.HoraApagado))
                {
                    var ahora = DateTime.Now;
                    var hoyStr = ahora.ToString("yyyy-MM-dd");

                    // Validar si solo aplica de Lunes a Viernes
                    bool esFinDeSemana = ahora.DayOfWeek == DayOfWeek.Saturday || ahora.DayOfWeek == DayOfWeek.Sunday;
                    if (!config.SoloLunesAViernes || !esFinDeSemana)
                    {
                        if (TimeOnly.TryParse(config.HoraApagado, out var horaTarget))
                        {
                            var horaActual = TimeOnly.FromDateTime(ahora);

                            // 1. Verificar si corresponde enviar aviso previo
                            var tiempoAviso = horaTarget.AddMinutes(-config.MinutosAvisoPrevio);
                            if (horaActual >= tiempoAviso && horaActual < horaTarget && _ultimoAvisoEnviadoDia != hoyStr)
                            {
                                var diffMinutos = (int)Math.Max(1, (horaTarget.ToTimeSpan() - horaActual.ToTimeSpan()).TotalMinutes);
                                var mensajeAviso = $"⚠️ AVISO INSTITUCIONAL: Todas las computadoras se apagarán automáticamente a las {config.HoraApagado} (en aprox. {diffMinutos} minutos) por cierre nocturno. Guarde sus documentos y cierre sesión.";
                                await _shutdownManager.EnviarAlertaPreventivaCampusAsync(_scopeFactory, mensajeAviso, stoppingToken);
                                _ultimoAvisoEnviadoDia = hoyStr;
                                _logger.LogInformation("Alerta preventiva de apagado nocturno emitida a todas las terminales ({Minutos} min antes).", diffMinutos);
                            }

                            // 2. Verificar si corresponde ejecutar el apagado masivo
                            if (horaActual >= horaTarget && config.UltimaEjecucionFecha != hoyStr)
                            {
                                _logger.LogWarning("Hora de cierre alcanzada ({Hora}). Ejecutando APAGADO MASIVO NOCTURNO EN TODO EL CAMPUS...", config.HoraApagado);
                                var motivo = $"Apagado nocturno automático del campus programado a las {config.HoraApagado}";
                                await _shutdownManager.EjecutarApagadoCampusAsync(_scopeFactory, motivo, stoppingToken);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ciclo de ejecución de ApagadoProgramadoBackgroundService.");
            }

            // Evaluar cada 30 segundos
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
