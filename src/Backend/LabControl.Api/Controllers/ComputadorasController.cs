using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Computadoras.Commands.AutoRegistrarComputadora;
using LabControl.Application.Features.Computadoras.Commands.CreateComputadora;
using LabControl.Application.Features.Computadoras.Queries.GetEstadoAulasMapa;

namespace LabControl.Api.Controllers;

public class ComputadorasController : ApiControllerBase
{
    [HttpGet("mapa-aulas")]
    public async Task<IActionResult> GetMapaAulas(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetEstadoAulasMapaQuery(), cancellationToken);
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
}
