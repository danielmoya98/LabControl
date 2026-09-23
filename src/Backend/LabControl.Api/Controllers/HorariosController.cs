using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
using LabControl.Application.Features.Horarios.Commands.UpdateBloqueHorario;
using LabControl.Application.Features.Horarios.Commands.DeleteBloqueHorario;
using LabControl.Application.Features.Horarios.Queries.GetHorariosByAula;

namespace LabControl.Api.Controllers;

public class HorariosController : ApiControllerBase
{
    [HttpGet("{aulaId:int}")]
    public async Task<IActionResult> GetHorariosByAula(int aulaId, [FromQuery] int? periodoId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetHorariosByAulaQuery(aulaId, periodoId), cancellationToken);
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

    [HttpGet("{id:int}/asistencia/pdf")]
    public async Task<IActionResult> GetReporteAsistenciaPdf(
        int id, 
        [FromQuery] DateTime? fecha,
        [FromServices] LabControl.Application.Common.Interfaces.IReportePdfService pdfService,
        CancellationToken cancellationToken)
    {
        var query = new LabControl.Application.Features.Horarios.Queries.GetReporteAsistenciaClase.GetReporteAsistenciaClaseQuery(id, fecha);
        var result = await Mediator.Send(query, cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error.Message });

        var bytes = pdfService.GenerarReporteAsistenciaClase(result.Value);
        var fechaStr = (fecha ?? DateTime.UtcNow).ToString("yyyyMMdd");
        var fileName = $"Asistencia_{result.Value.AulaNombre.Replace(" ", "_")}_{fechaStr}.pdf";
        return File(bytes, "application/pdf", fileName);
    }
}
