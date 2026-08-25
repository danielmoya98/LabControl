using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
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
}
