using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Computadoras.Commands.AutoRegistrarComputadora;
using LabControl.Application.Features.Computadoras.Commands.CreateComputadora;
using LabControl.Application.Features.Computadoras.Commands.UpdateComputadora;
using LabControl.Application.Features.Computadoras.Commands.DeleteComputadora;
using LabControl.Application.Features.Computadoras.Queries.GetEstadoAulasMapa;
using LabControl.Application.Features.Computadoras.Queries.GetComputadorasByAula;

namespace LabControl.Api.Controllers;

public class ComputadorasController : ApiControllerBase
{
    [HttpGet("mapa-aulas")]
    public async Task<IActionResult> GetMapaAulas(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetEstadoAulasMapaQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("aula/{aulaId:int}")]
    public async Task<IActionResult> GetComputadorasByAula(int aulaId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetComputadorasByAulaQuery(aulaId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateComputadora([FromBody] CreateComputadoraCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("auto-registro")]
    public async Task<IActionResult> AutoRegistrarComputadora([FromBody] AutoRegistrarComputadoraCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateComputadora(int id, [FromBody] UpdateComputadoraCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "El ID de la ruta no coincide con el cuerpo de la petición." });
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteComputadora(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteComputadoraCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("{id:int}/energia")]
    public async Task<IActionResult> EnviarComandoEnergia(
        int id,
        [FromBody] ComandoEnergiaRequest request,
        [FromServices] LabControl.Application.Common.Interfaces.ISignalRNotificationService notificationService,
        [FromServices] LabControl.Application.Common.Interfaces.IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var pc = await context.Computadoras.FindAsync([id], cancellationToken);
        if (pc == null) return NotFound(new { error = "Computadora no encontrada." });

        var cmd = request.TipoComando?.Trim().ToUpperInvariant() ?? "SHUTDOWN";
        var motivo = string.IsNullOrWhiteSpace(request.Motivo) ? "Comando ejecutado desde Panel Administrativo" : request.Motivo;

        await notificationService.SendComandoEnergiaTerminalAsync(pc.Hostname, cmd, motivo, cancellationToken);
        return Ok(new { message = $"Comando {cmd} enviado a la terminal {pc.Hostname}." });
    }

    [HttpPost("{id:int}/wake-on-lan")]
    public async Task<IActionResult> WakeOnLan(
        int id,
        [FromServices] LabControl.Application.Common.Interfaces.IWakeOnLanService wolService,
        [FromServices] LabControl.Application.Common.Interfaces.IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var pc = await context.Computadoras.FindAsync([id], cancellationToken);
        if (pc == null) return NotFound(new { error = "Computadora no encontrada." });

        if (string.IsNullOrWhiteSpace(pc.MacAddress))
        {
            return BadRequest(new { error = $"La terminal '{pc.Hostname}' no tiene una dirección MAC física registrada para Wake-on-LAN." });
        }

        var exito = await wolService.EnviarMagicPacketAsync(pc.MacAddress, cancellationToken);
        if (exito)
        {
            return Ok(new { message = $"Paquete de encendido Wake-on-LAN enviado a '{pc.Hostname}' (MAC: {pc.MacAddress})." });
        }

        return StatusCode(500, new { error = $"Error al emitir el paquete Wake-on-LAN a la dirección MAC '{pc.MacAddress}'." });
    }
}

public record ComandoEnergiaRequest(string? TipoComando = "SHUTDOWN", string? Motivo = null);

