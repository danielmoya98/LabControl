using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
using LabControl.Application.Features.Horarios.Commands.UpdateBloqueHorario;
using LabControl.Application.Features.Horarios.Commands.DeleteBloqueHorario;
using LabControl.Application.Features.Horarios.Queries.GetHorariosByAula;

namespace LabControl.Api.Controllers;

public class HorariosController : ApiControllerBase
{
    [HttpGet("{aulaId:int}")]
    public async Task<IActionResult> GetHorariosByAula(int aulaId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetHorariosByAulaQuery(aulaId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBloqueHorario([FromBody] CreateBloqueHorarioCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBloqueHorario(int id, [FromBody] UpdateBloqueHorarioCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "El ID de la ruta no coincide con el cuerpo de la petición." });
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBloqueHorario(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteBloqueHorarioCommand(id), cancellationToken);
        return HandleResult(result);
    }
}
