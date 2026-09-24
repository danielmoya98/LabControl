using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;
using Serilog;

namespace LabControl.Api.Controllers;

public record DesloguearRequest(string? Hostname, int? AulaId, TipoCierreSesion Motivo = TipoCierreSesion.AdminRemoto);
public record EnviarMensajeRequest(string Mensaje, string? Hostname = null, int? AulaId = null);
public record HeartbeatRequest(string Hostname, string MacAddress, string Ip);
public record ReportarApagadoForzadoRequest(string Hostname, DateTime? FechaHoraEventoUtc, int EventId, string? Detalle, string? UltimoEmailDetectado = null);

[ApiController]
[Route("api/control")]
public class ControlRemotoController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ISignalRNotificationService _signalR;

    public ControlRemotoController(IApplicationDbContext context, ISignalRNotificationService signalR)
    {
        _context = context;
        _signalR = signalR;
    }

    [HttpPost("desloguear")]
    public async Task<IActionResult> Desloguear([FromBody] DesloguearRequest request, CancellationToken cancellationToken)
    {
        var ahora = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Hostname))
        {
            var hostNorm = request.Hostname.Trim().ToUpperInvariant();
            var pc = await _context.Computadoras
                .FirstOrDefaultAsync(c => c.Hostname == hostNorm, cancellationToken);

            if (pc == null)
            {
                return NotFound(new { error = $"No se encontró la computadora '{request.Hostname}'." });
            }

            var sesionActiva = await _context.SesionesUso
                .Where(s => s.ComputadoraId == pc.Id && s.FechaHoraFin == null)
                .OrderByDescending(s => s.FechaHoraInicio)
                .FirstOrDefaultAsync(cancellationToken);

            if (sesionActiva != null)
            {
                sesionActiva.Finalizar(request.Motivo, ahora);
            }

            pc.CambiarEstado(EstadoComputadora.Disponible);
            await _context.SaveChangesAsync(cancellationToken);

            // Enviar orden de cierre por SignalR a la máquina física
            await _signalR.SendComandoCierreSesionAsync(pc.Hostname, request.Motivo, cancellationToken);
            // Notificar al WebAdmin
            await _signalR.NotifyEstadoComputadoraCambiadoAsync(pc.Id, pc.Hostname, EstadoComputadora.Disponible, null, cancellationToken);

            return Ok(new { mensaje = $"Orden de cierre de sesión enviada exitosamente a la terminal '{pc.Hostname}'." });
        }
        else if (request.AulaId.HasValue && request.AulaId.Value > 0)
        {
            var pcs = await _context.Computadoras
                .Where(c => c.AulaId == request.AulaId.Value)
                .ToListAsync(cancellationToken);

            var pcsIds = pcs.Select(p => p.Id).ToList();

            var sesionesActivas = await _context.SesionesUso
                .Where(s => pcsIds.Contains(s.ComputadoraId) && s.FechaHoraFin == null)
                .ToListAsync(cancellationToken);

            foreach (var sesion in sesionesActivas)
            {
                sesion.Finalizar(request.Motivo, ahora);
            }

            foreach (var pc in pcs)
            {
                pc.CambiarEstado(EstadoComputadora.Disponible);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Enviar orden de cierre a toda el aula vía SignalR
            await _signalR.SendComandoCierreSesionAulaAsync(request.AulaId.Value, request.Motivo, cancellationToken);

            // Actualizar tarjetas en WebAdmin
            foreach (var pc in pcs)
            {
                await _signalR.NotifyEstadoComputadoraCambiadoAsync(pc.Id, pc.Hostname, EstadoComputadora.Disponible, null, cancellationToken);
            }

            return Ok(new { mensaje = $"Orden de deslogueo masivo procesada para {pcs.Count} terminales del Aula {request.AulaId.Value}." });
        }
        else
        {
            // Deslogueo masivo general
            var pcs = await _context.Computadoras.ToListAsync(cancellationToken);
            var sesionesActivas = await _context.SesionesUso
                .Where(s => s.FechaHoraFin == null)
                .ToListAsync(cancellationToken);

            foreach (var sesion in sesionesActivas)
            {
                sesion.Finalizar(request.Motivo, ahora);
            }

            foreach (var pc in pcs)
            {
                pc.CambiarEstado(EstadoComputadora.Disponible);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _signalR.SendComandoCierreSesionGlobalAsync(request.Motivo, cancellationToken);

            foreach (var pc in pcs)
            {
                await _signalR.NotifyEstadoComputadoraCambiadoAsync(pc.Id, pc.Hostname, EstadoComputadora.Disponible, null, cancellationToken);
            }

            return Ok(new { mensaje = "Orden de deslogueo global procesada para todas las terminales del centro de cómputo." });
        }
    }

    [HttpPost("enviar-mensaje")]
    public async Task<IActionResult> EnviarMensaje([FromBody] EnviarMensajeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Mensaje))
        {
            return BadRequest(new { error = "El contenido del mensaje no puede estar vacío." });
        }

        if (!string.IsNullOrWhiteSpace(request.Hostname))
        {
            await _signalR.SendAlertaTerminalAsync(request.Hostname, request.Mensaje.Trim(), cancellationToken);
            return Ok(new { mensaje = $"Mensaje enviado a la terminal '{request.Hostname}'." });
        }
        else if (request.AulaId.HasValue && request.AulaId.Value > 0)
        {
            await _signalR.SendAlertaAulaAsync(request.AulaId.Value, request.Mensaje.Trim(), cancellationToken);
            return Ok(new { mensaje = $"Mensaje transmitido a toda el Aula {request.AulaId.Value}." });
        }
        else
        {
            await _signalR.SendAlertaGlobalAsync(request.Mensaje.Trim(), cancellationToken);
            return Ok(new { mensaje = "Mensaje transmitido a todas las terminales activas." });
        }
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> RegistrarHeartbeat([FromBody] HeartbeatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Hostname))
        {
            return BadRequest(new { error = "Hostname obligatorio." });
        }

        var hostNorm = request.Hostname.Trim().ToUpperInvariant();
        var pc = await _context.Computadoras
            .FirstOrDefaultAsync(c => c.Hostname == hostNorm, cancellationToken);

        if (pc == null)
        {
            return NotFound(new { error = "Terminal no encontrada en el sistema." });
        }

        var estabaOffline = pc.EstadoActual == EstadoComputadora.Offline;
        var ipResult = IpAddress.Create(request.Ip);
        if (ipResult.IsSuccess)
        {
            pc.ActualizarHeartbeat(ipResult.Value);
        }

        string? emailEstudiante = null;
        if (estabaOffline)
        {
            var sesionActiva = await _context.SesionesUso
                .Where(s => s.ComputadoraId == pc.Id && s.FechaHoraFin == null)
                .OrderByDescending(s => s.FechaHoraInicio)
                .FirstOrDefaultAsync(cancellationToken);

            if (sesionActiva != null)
            {
                pc.CambiarEstado(EstadoComputadora.EnUso);
                emailEstudiante = sesionActiva.EmailEstudiante;
            }
            else
            {
                pc.CambiarEstado(EstadoComputadora.Disponible);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (estabaOffline)
        {
            await _signalR.NotifyEstadoComputadoraCambiadoAsync(pc.Id, pc.Hostname, pc.EstadoActual, emailEstudiante, cancellationToken);
        }

        return Ok(new { ok = true, estado = pc.EstadoActual.ToString() });
    }

    [HttpPost("reportar-apagado-forzado")]
    public async Task<IActionResult> ReportarApagadoForzado([FromBody] ReportarApagadoForzadoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Hostname))
        {
            return BadRequest(new { error = "Hostname requerido." });
        }

        var hostNorm = request.Hostname.Trim().ToUpperInvariant();
        var pc = await _context.Computadoras
            .FirstOrDefaultAsync(c => c.Hostname == hostNorm, cancellationToken);

        if (pc == null)
        {
            return NotFound(new { error = $"No se encontró la computadora '{request.Hostname}'." });
        }

        var fechaEvento = request.FechaHoraEventoUtc ?? DateTime.UtcNow;

        // Buscar si había una sesión abierta en esta máquina
        var sesion = await _context.SesionesUso
            .Where(s => s.ComputadoraId == pc.Id && s.FechaHoraFin == null)
            .OrderByDescending(s => s.FechaHoraInicio)
            .FirstOrDefaultAsync(cancellationToken);

        string emailAfectado = sesion?.EmailEstudiante 
            ?? (!string.IsNullOrWhiteSpace(request.UltimoEmailDetectado) ? request.UltimoEmailDetectado : (pc.UltimoEstudianteEmail ?? "sin-usuario@est.univalle.edu"));

        if (sesion != null)
        {
            sesion.Finalizar(TipoCierreSesion.ApagadoForzado, fechaEvento);
        }

        // Registrar o certificar el incidente de energía con evidencia de Event Log
        var motivo = $"Apagado abrupto de fuerza bruta confirmado por Windows Event Log (Event ID {request.EventId}): {request.Detalle ?? "Corte de energía / Kernel-Power"}";
        
        var regEnergia = RegistroConsumoEnergia.Create(
            pc.Id,
            pc.AulaId,
            emailAfectado,
            null,
            0.1,
            motivo,
            sesion?.Id
        );

        if (regEnergia.IsSuccess)
        {
            _context.RegistrosConsumoEnergia.Add(regEnergia.Value);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { ok = true, mensaje = "Incidente de apagado forzado auditado correctamente en la base de datos." });
    }

    [HttpPost("sondear")]
    public async Task<IActionResult> SondearTerminales(
        [FromQuery] int? aulaId,
        [FromQuery] string? hostname,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(hostname))
        {
            var hostNorm = hostname.Trim().ToUpperInvariant();
            await _signalR.SendSolicitudHeartbeatTerminalAsync(hostNorm, cancellationToken);
            return Ok(new { ok = true, mensaje = $"Sondeo de telemetría y heartbeat emitido a la terminal '{hostNorm}'." });
        }

        if (aulaId.HasValue && aulaId.Value > 0)
        {
            await _signalR.SendSolicitudHeartbeatAulaAsync(aulaId.Value, cancellationToken);
        }
        else
        {
            await _signalR.SendSolicitudHeartbeatGlobalAsync(cancellationToken);
        }

        // Evaluar terminales que no han emitido heartbeat reciente (umbral: 60 segundos)
        var umbralOffline = DateTime.UtcNow.AddSeconds(-60);
        var query = _context.Computadoras.AsQueryable();
        if (aulaId.HasValue && aulaId.Value > 0)
        {
            query = query.Where(c => c.AulaId == aulaId.Value);
        }

        var pcs = await query.ToListAsync(cancellationToken);
        int marcadasOffline = 0;
        foreach (var pc in pcs)
        {
            if (pc.UltimoHeartbeatUtc.HasValue && pc.UltimoHeartbeatUtc.Value < umbralOffline && pc.EstadoActual != EstadoComputadora.Offline)
            {
                pc.CambiarEstado(EstadoComputadora.Offline);
                marcadasOffline++;
                await _signalR.NotifyEstadoComputadoraCambiadoAsync(pc.Id, pc.Hostname, EstadoComputadora.Offline, null, cancellationToken);
            }
        }

        if (marcadasOffline > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            ok = true,
            mensaje = "Sondeo de telemetría y heartbeat transmitido con éxito.",
            terminalesEvaluadas = pcs.Count,
            marcadasOffline
        });
    }
}
