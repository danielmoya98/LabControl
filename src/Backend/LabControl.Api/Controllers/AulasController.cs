using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
using LabControl.Application.Features.Aulas.Commands.UpdateAula;
using LabControl.Application.Features.Aulas.Commands.DeleteAula;
using LabControl.Application.Features.Aulas.Queries.GetAulas;

namespace LabControl.Api.Controllers;

public class AulasController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAulas([FromQuery] int? bloqueId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAulasQuery(bloqueId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAula([FromBody] CreateAulaCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAula(int id, [FromBody] UpdateAulaCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "El ID de la ruta no coincide con el cuerpo de la petición." });
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAula(int id, [FromQuery] bool softDelete = true, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new DeleteAulaCommand(id, softDelete), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("{id:int}/apagar-todo")]
    public async Task<IActionResult> ApagarAula(
        int id,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] LabControl.Api.Controllers.ComandoEnergiaRequest? request = null,
        [FromServices] LabControl.Application.Common.Interfaces.ISignalRNotificationService notificationService = default!,
        [FromServices] LabControl.Application.Common.Interfaces.IApplicationDbContext context = default!,
        CancellationToken cancellationToken = default)
    {
        var aula = await context.Aulas.FindAsync([id], cancellationToken);
        if (aula == null) return NotFound(new { error = "Aula no encontrada." });

        var motivo = string.IsNullOrWhiteSpace(request?.Motivo) ? $"Apagado masivo solicitado para el aula {aula.Nombre}" : request.Motivo;
        
        // 1. Enviar orden al grupo del aula completa
        await notificationService.SendComandoEnergiaAulaAsync(id, "SHUTDOWN", motivo, cancellationToken);

        // 2. Redundancia directa a cada terminal registrada en dicha aula
        var pcs = await context.Computadoras
            .Where(c => c.AulaId == id)
            .Select(c => c.Hostname)
            .ToListAsync(cancellationToken);

        foreach (var hostname in pcs)
        {
            await notificationService.SendComandoEnergiaTerminalAsync(hostname, "SHUTDOWN", motivo, cancellationToken);
        }

        return Ok(new { message = $"Orden de apagado general transmitida al aula '{aula.Nombre}' ({pcs.Count} terminales)." });
    }

    [HttpPost("{id:int}/wake-on-lan")]
    public async Task<IActionResult> WakeOnLanAula(
        int id,
        [FromServices] LabControl.Application.Common.Interfaces.IWakeOnLanService wolService,
        [FromServices] LabControl.Application.Common.Interfaces.IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var aula = await context.Aulas.FindAsync([id], cancellationToken);
        if (aula == null) return NotFound(new { error = "Aula no encontrada." });

        var macs = await context.Computadoras
            .Where(c => c.AulaId == id && !string.IsNullOrWhiteSpace(c.MacAddress))
            .Select(c => c.MacAddress!)
            .ToListAsync(cancellationToken);

        if (!macs.Any())
        {
            return BadRequest(new { error = $"El aula '{aula.Nombre}' no tiene computadoras con dirección MAC registrada." });
        }

        var enviados = await wolService.EnviarMagicPacketBatchAsync(macs, cancellationToken);
        return Ok(new { message = $"Se enviaron paquetes Wake-on-LAN a {enviados} de {macs.Count} computadoras del aula '{aula.Nombre}'." });
    }

    [HttpPost("{id:int}/modo-evento")]
    public async Task<IActionResult> ModoEventoAula(
        int id,
        [FromBody] ModoEventoRequest request,
        [FromServices] LabControl.Application.Common.Interfaces.ISignalRNotificationService notificationService,
        [FromServices] LabControl.Application.Common.Interfaces.IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var aula = await context.Aulas.FindAsync([id], cancellationToken);
        if (aula == null) return NotFound(new { error = "Aula no encontrada." });

        if (request.Activar)
        {
            var nombreEvento = string.IsNullOrWhiteSpace(request.Motivo) ? "Evento / Invitados Especiales" : request.Motivo.Trim();
            aula.IniciarModoEvento(nombreEvento, request.DuracionMinutos);
        }
        else
        {
            aula.FinalizarModoEvento();
        }

        await context.SaveChangesAsync(cancellationToken);

        // Notificar por SignalR a todas las terminales del aula
        await notificationService.SendComandoModoEventoAulaAsync(
            aula.Id,
            request.Activar,
            request.Motivo ?? (request.Activar ? "Evento / Invitados" : "Fin de Evento"),
            request.DuracionMinutos,
            cancellationToken);

        var msg = request.Activar
            ? $"Modo Evento activado en el aula '{aula.Nombre}' ({(request.DuracionMinutos > 0 ? $"{request.DuracionMinutos} min" : "Indefinido")}). Las computadoras han sido desbloqueadas."
            : $"Modo Evento finalizado en el aula '{aula.Nombre}'. El bloqueo Kiosk ha sido restablecido.";

        return Ok(new { message = msg, aula.ModoEventoActivo, aula.ModoEventoFinUtc, aula.ModoEventoNombre });
    }

    [HttpPost("{id:int}/freeze")]
    public async Task<IActionResult> FreezeAula(
        int id,
        [FromBody] ComandoFreezeRequest request,
        [FromServices] LabControl.Application.Common.Interfaces.ISignalRNotificationService notificationService,
        [FromServices] LabControl.Application.Common.Interfaces.IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var aula = await context.Aulas.FindAsync([id], cancellationToken);
        if (aula == null) return NotFound(new { error = "Aula no encontrada." });

        var accion = request.Accion?.Trim().ToUpperInvariant() ?? "THAW";
        await notificationService.SendComandoFreezeAulaAsync(id, accion, request.Clave, cancellationToken);

        var pcs = await context.Computadoras
            .Where(c => c.AulaId == id)
            .Select(c => c.Hostname)
            .ToListAsync(cancellationToken);

        foreach (var host in pcs)
        {
            await notificationService.SendComandoFreezeTerminalAsync(host, accion, request.Clave, cancellationToken);
        }

        var textoAccion = accion == "THAW" ? "Descongelar (Thaw)" : "Congelar (Freeze)";
        return Ok(new { message = $"Comando {textoAccion} transmitido a {pcs.Count} terminales del aula '{aula.Nombre}'. Los equipos aplicarán el comando y se reiniciarán." });
    }

    [HttpPost("{id:int}/actualizar-clientes")]
    public async Task<IActionResult> ActualizarClientesAula(
        int id,
        [FromBody] ComandoActualizacionRequest request,
        [FromServices] LabControl.Application.Common.Interfaces.ISignalRNotificationService notificationService,
        [FromServices] LabControl.Application.Common.Interfaces.IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var aula = await context.Aulas.FindAsync([id], cancellationToken);
        if (aula == null) return NotFound(new { error = "Aula no encontrada." });

        var url = string.IsNullOrWhiteSpace(request.UrlDescarga) ? "api/actualizaciones/descargar-cliente" : request.UrlDescarga;
        await notificationService.SendComandoActualizacionAulaAsync(id, url, request.NuevaVersion ?? "latest", request.Sha256 ?? "", cancellationToken);

        var pcs = await context.Computadoras
            .Where(c => c.AulaId == id)
            .Select(c => c.Hostname)
            .ToListAsync(cancellationToken);

        foreach (var host in pcs)
        {
            await notificationService.SendComandoActualizacionTerminalAsync(host, url, request.NuevaVersion ?? "latest", request.Sha256 ?? "", cancellationToken);
        }

        return Ok(new { message = $"Orden de actualización masiva enviada a {pcs.Count} terminales del aula '{aula.Nombre}' (Versión: {request.NuevaVersion ?? "latest"})." });
    }
}

public record ModoEventoRequest(bool Activar, int DuracionMinutos = 0, string? Motivo = null);

