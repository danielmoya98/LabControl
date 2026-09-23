using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;
using Serilog;

namespace LabControl.Api.Services;

public class HeartbeatMonitorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly TimeSpan IntervaloChequeo = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan UmbralInactividad = TimeSpan.FromSeconds(75);

    public HeartbeatMonitorBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Iniciando servicio en segundo plano: HeartbeatMonitorBackgroundService.");

        using var timer = new PeriodicTimer(IntervaloChequeo);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await VerificarTerminalesInactivasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al verificar latidos de terminales en HeartbeatMonitorBackgroundService.");
            }
        }
    }

    private async Task VerificarTerminalesInactivasAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var signalR = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();

        var fechaLimite = DateTime.UtcNow.Subtract(UmbralInactividad);

        var terminalesCaidas = await context.Computadoras
            .Where(c => c.EstadoActual != EstadoComputadora.Offline && 
                        c.UltimoHeartbeatUtc != null && 
                        c.UltimoHeartbeatUtc < fechaLimite)
            .ToListAsync(cancellationToken);

        if (terminalesCaidas.Count == 0) return;

        foreach (var pc in terminalesCaidas)
        {
            Log.Warning("Terminal {Hostname} (ID: {Id}) no ha emitido heartbeat desde {UltimoHeartbeat}. Marcándola como Offline.",
                pc.Hostname, pc.Id, pc.UltimoHeartbeatUtc);

            pc.CambiarEstado(EstadoComputadora.Offline);

            // Verificar si la terminal tenía una sesión activa que quedó huérfana por apagón forzado
            var sesionActiva = await context.SesionesUso
                .FirstOrDefaultAsync(s => s.ComputadoraId == pc.Id && s.FechaHoraFin == null, cancellationToken);

            if (sesionActiva != null)
            {
                var fechaFin = pc.UltimoHeartbeatUtc ?? DateTime.UtcNow;
                sesionActiva.Finalizar(TipoCierreSesion.ApagadoForzado, fechaFin);

                var duracionHoras = Math.Max(0.1, Math.Round((fechaFin - sesionActiva.FechaHoraInicio).TotalHours, 1));
                var regEnergia = RegistroConsumoEnergia.Create(
                    pc.Id,
                    pc.AulaId,
                    sesionActiva.EmailEstudiante,
                    null,
                    duracionHoras,
                    "Equipo apagado forzadamente o corte abrupto de energía con sesión abierta (Detectado por Watchdog de Servidor)",
                    sesionActiva.Id
                );

                if (regEnergia.IsSuccess)
                {
                    context.RegistrosConsumoEnergia.Add(regEnergia.Value);
                }

                Log.Warning("Sesión {SesionId} de {Email} cerrada automáticamente por desconexión/apagado abrupto de {Hostname}.",
                    sesionActiva.Id, sesionActiva.EmailEstudiante, pc.Hostname);
            }

            await signalR.NotifyEstadoComputadoraCambiadoAsync(
                pc.Id,
                pc.Hostname,
                EstadoComputadora.Offline,
                null,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
