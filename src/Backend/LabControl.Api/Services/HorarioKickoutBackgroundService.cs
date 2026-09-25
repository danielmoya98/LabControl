using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Enums;
using Serilog;

namespace LabControl.Api.Services;

/// <summary>
/// Servicio en segundo plano para supervisar horarios académicos y franjas de recreo,
/// emitiendo avisos preventivos de 5 minutos y ejecutando el auto-kickout de sesiones al concluir los períodos.
/// </summary>
public class HorarioKickoutBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly TimeSpan IntervaloChequeo = TimeSpan.FromSeconds(30);
    private readonly HashSet<string> _eventosProcesados = new();
    private DateTime _fechaUltimaLimpieza = DateTime.UtcNow.Date;

    public HorarioKickoutBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Iniciando servicio en segundo plano: HorarioKickoutBackgroundService.");

        using var timer = new PeriodicTimer(IntervaloChequeo);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcesarHorariosYRecreosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al procesar auto-kickout de horarios en HorarioKickoutBackgroundService.");
            }
        }
    }

    private async Task ProcesarHorariosYRecreosAsync(CancellationToken cancellationToken)
    {
        // Limpieza diaria de la caché en memoria para evitar acumulación
        if (DateTime.UtcNow.Date > _fechaUltimaLimpieza)
        {
            _eventosProcesados.Clear();
            _fechaUltimaLimpieza = DateTime.UtcNow.Date;
        }

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var signalR = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();

        var ahora = TimeZoneHelper.NowBolivia;
        var horaActual = ahora.TimeOfDay;
        var diaActual = ObtenerDiaSemana(ahora.DayOfWeek);
        var claveFecha = ahora.ToString("yyyyMMdd");

        // Obtener todos los bloques horarios configurados para el día de hoy
        var bloquesHoy = await context.BloquesHorarios
            .Include(b => b.Aula)
            .Where(b => b.DiaSemana == diaActual)
            .ToListAsync(cancellationToken);

        if (bloquesHoy.Count == 0) return;

        foreach (var bloque in bloquesHoy)
        {
            // ==================== 1. PRE-AVISO DE 5 MINUTOS ====================
            var ventanaAvisoInicio = bloque.HoraFin.Subtract(TimeSpan.FromMinutes(5));
            var ventanaAvisoFin = bloque.HoraFin.Subtract(TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(30)));

            if (horaActual >= ventanaAvisoInicio && horaActual < ventanaAvisoFin)
            {
                string claveAviso = $"{bloque.Id}_{claveFecha}_aviso5min";
                if (_eventosProcesados.Add(claveAviso))
                {
                    string mensaje = bloque.EsRecreo
                        ? "☕ Estimados estudiantes: El período de receso concluirá en 5 minutos."
                        : $"📢 Estimados estudiantes del {bloque.Aula.Nombre}: El período actual ({bloque.Descripcion ?? "Clase"}) finaliza en 5 minutos. Por favor guarden sus archivos.";

                    Log.Information("Emitiendo pre-aviso de 5 minutos al aula {AulaNombre} (Bloque {BloqueId}).",
                        bloque.Aula.Nombre, bloque.Id);

                    await signalR.SendAlertaAulaAsync(bloque.AulaId, mensaje, cancellationToken);
                }
            }

            // ==================== 2. AUTO-KICKOUT AL FINALIZAR EL PERÍODO ====================
            var ventanaCierreFin = bloque.HoraFin.Add(TimeSpan.FromMinutes(2));

            if (horaActual >= bloque.HoraFin && horaActual <= ventanaCierreFin)
            {
                string claveCierre = $"{bloque.Id}_{claveFecha}_cierre";
                if (_eventosProcesados.Add(claveCierre))
                {
                    var motivo = bloque.EsRecreo ? TipoCierreSesion.Recreo : TipoCierreSesion.FinPeriodo;

                    await EjecutarCierreSesionesAulaAsync(
                        context,
                        signalR,
                        bloque.AulaId,
                        bloque.Aula.Nombre,
                        motivo,
                        cancellationToken);
                }
            }

            // ==================== 3. EXPULSIÓN ACTIVA DURANTE FRANJA DE RECREO ====================
            if (bloque.EsRecreo && horaActual >= bloque.HoraInicio && horaActual < bloque.HoraFin)
            {
                // Si estamos en medio de un recreo y hay sesiones abiertas, cerrarlas
                var haySesionesAbiertas = await context.SesionesUso
                    .AnyAsync(s => s.Computadora.AulaId == bloque.AulaId && s.FechaHoraFin == null, cancellationToken);

                if (haySesionesAbiertas)
                {
                    await EjecutarCierreSesionesAulaAsync(
                        context,
                        signalR,
                        bloque.AulaId,
                        bloque.Aula.Nombre,
                        TipoCierreSesion.Recreo,
                        cancellationToken);
                }
            }
        }
    }

    private static async Task EjecutarCierreSesionesAulaAsync(
        IApplicationDbContext context,
        ISignalRNotificationService signalR,
        int aulaId,
        string aulaNombre,
        TipoCierreSesion motivo,
        CancellationToken cancellationToken)
    {
        var sesionesActivas = await context.SesionesUso
            .Include(s => s.Computadora)
            .Where(s => s.Computadora.AulaId == aulaId && s.FechaHoraFin == null)
            .ToListAsync(cancellationToken);

        if (sesionesActivas.Count == 0) return;

        Log.Information("Ejecutando auto-kickout de {Cantidad} sesiones activas en Aula {AulaNombre} con motivo {Motivo}.",
            sesionesActivas.Count, aulaNombre, motivo);

        var ahoraUtc = DateTime.UtcNow;

        foreach (var sesion in sesionesActivas)
        {
            sesion.Finalizar(motivo, ahoraUtc);
            sesion.Computadora.CambiarEstado(EstadoComputadora.Disponible);

            // Notificar a los paneles administrativos del cambio a Disponible
            await signalR.NotifyEstadoComputadoraCambiadoAsync(
                sesion.Computadora.Id,
                sesion.Computadora.Hostname,
                EstadoComputadora.Disponible,
                null,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Emitir comando SignalR de cierre forzado a todas las terminales del aula
        await signalR.SendComandoCierreSesionAulaAsync(aulaId, motivo, cancellationToken);
    }

    private static DiaSemana ObtenerDiaSemana(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => DiaSemana.Lunes,
        DayOfWeek.Tuesday => DiaSemana.Martes,
        DayOfWeek.Wednesday => DiaSemana.Miercoles,
        DayOfWeek.Thursday => DiaSemana.Jueves,
        DayOfWeek.Friday => DiaSemana.Viernes,
        DayOfWeek.Saturday => DiaSemana.Sabado,
        _ => DiaSemana.Domingo
    };
}
