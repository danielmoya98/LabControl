using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Docentes.Commands.CreateDocente;
using LabControl.Application.Features.Docentes.Commands.UpdateDocente;
using LabControl.Application.Features.Docentes.Commands.DeleteDocente;
using LabControl.Application.Features.Docentes.Queries.GetDocentes;

namespace LabControl.Api.Controllers;

[Route("api/[controller]")]
public class DocentesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDocentes(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDocentesQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDocente([FromBody] CreateDocenteCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDocente(int id, [FromBody] UpdateDocenteCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "El ID de la ruta no coincide con el cuerpo de la petición." });
        }
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDocente(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteDocenteCommand(id), cancellationToken);
        return HandleResult(result);
    }
}
