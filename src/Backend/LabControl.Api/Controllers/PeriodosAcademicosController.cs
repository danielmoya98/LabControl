using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.PeriodosAcademicos.Commands.ClonarHorariosSemestre;
using LabControl.Application.Features.PeriodosAcademicos.Commands.CreatePeriodoAcademico;
using LabControl.Application.Features.PeriodosAcademicos.Commands.SetPeriodoAcademicoActual;
using LabControl.Application.Features.PeriodosAcademicos.Queries.GetPeriodosAcademicos;
using LabControl.Api.Controllers;

[Route("api/periodos-academicos")]
[Route("api/[controller]")]
public class PeriodosAcademicosController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPeriodosAcademicos([FromQuery] bool soloActivos = false, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetPeriodosAcademicosQuery(soloActivos), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePeriodoAcademico([FromBody] CreatePeriodoAcademicoCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:int}/activar")]
    public async Task<IActionResult> SetPeriodoActual(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetPeriodoAcademicoActualCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("clonar")]
    public async Task<IActionResult> ClonarHorarios([FromBody] ClonarHorariosSemestreCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
