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
}
