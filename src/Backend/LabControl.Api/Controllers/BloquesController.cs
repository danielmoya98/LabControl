using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Bloques.Commands.CreateBloque;
using LabControl.Application.Features.Bloques.Queries.GetBloques;

namespace LabControl.Api.Controllers;

[Route("api/[controller]")]
public class BloquesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBloques(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetBloquesQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBloque([FromBody] CreateBloqueCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
