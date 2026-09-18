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
}
