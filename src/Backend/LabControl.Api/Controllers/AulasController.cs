using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
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
}
