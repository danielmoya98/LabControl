using MediatR;
using LabControl.Application.Features.Energia.Commands.EvaluarEquiposEncendidos;

namespace LabControl.Api.Services;

public class AuditoriaEnergiaBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditoriaEnergiaBackgroundService> _logger;

    public AuditoriaEnergiaBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditoriaEnergiaBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Detección de Desperdicio Energético iniciado.");

        // Esperar 1 minuto tras el arranque antes de la primera evaluación
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var result = await mediator.Send(new EvaluarEquiposEncendidosCommand(), stoppingToken);
                if (result.IsSuccess && result.Value > 0)
                {
                    _logger.LogWarning("Se detectaron y registraron {Nuevos} equipos encendidos desatendidos sin apagar.", result.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar auditoría energética de equipos encendidos.");
            }

            // Evaluar cada 5 minutos
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
