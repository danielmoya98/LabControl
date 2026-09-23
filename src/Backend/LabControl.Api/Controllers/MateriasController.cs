using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Materias.Commands.CreateMateria;
using LabControl.Application.Features.Materias.Commands.UpdateMateria;
using LabControl.Application.Features.Materias.Commands.DeleteMateria;
using LabControl.Application.Features.Materias.Queries.GetMaterias;

namespace LabControl.Api.Controllers;

[Route("api/[controller]")]
public class MateriasController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMaterias(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMateriasQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMateria([FromBody] CreateMateriaCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMateria(int id, [FromBody] UpdateMateriaCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "El ID de la ruta no coincide con el cuerpo de la petición." });
        }
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMateria(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteMateriaCommand(id), cancellationToken);
        return HandleResult(result);
    }
}
