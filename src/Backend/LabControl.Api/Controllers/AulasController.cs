using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
using LabControl.Application.Features.Aulas.Commands.UpdateAula;
using LabControl.Application.Features.Aulas.Commands.DeleteAula;
using LabControl.Application.Features.Aulas.Queries.GetAulas;

namespace LabControl.Api.Controllers;

public class AulasController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAulas(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAulasQuery(), cancellationToken);
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
        [FromBody] LabControl.Api.Controllers.ComandoEnergiaRequest? request,
        [FromServices] LabControl.Application.Common.Interfaces.ISignalRNotificationService notificationService,
        [FromServices] LabControl.Application.Common.Interfaces.IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var aula = await context.Aulas.FindAsync([id], cancellationToken);
        if (aula == null) return NotFound(new { error = "Aula no encontrada." });

        var motivo = string.IsNullOrWhiteSpace(request?.Motivo) ? $"Apagado masivo solicitado para el aula {aula.Nombre}" : request.Motivo;
        await notificationService.SendComandoEnergiaAulaAsync(id, "SHUTDOWN", motivo, cancellationToken);
        return Ok(new { message = $"Orden de apagado general transmitida al aula '{aula.Nombre}'." });
    }
}
