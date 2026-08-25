using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Sesiones.Commands.SincronizarSesionesBatch;

namespace LabControl.Api.Controllers;

public class SesionesController : ApiControllerBase
{
    [HttpPost("sync-batch")]
    public async Task<IActionResult> SincronizarSesionesBatch([FromBody] SincronizarSesionesBatchCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
