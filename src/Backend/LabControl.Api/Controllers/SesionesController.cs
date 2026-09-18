using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Sesiones.Commands.IniciarSesion;
using LabControl.Application.Features.Sesiones.Commands.FinalizarSesion;
using LabControl.Application.Features.Sesiones.Commands.SincronizarSesionesBatch;
using LabControl.Application.Features.Sesiones.Queries.GetSesionesAuditoria;
using LabControl.Domain.Enums;

namespace LabControl.Api.Controllers;

public class SesionesController : ApiControllerBase
{
    private readonly IReporteExcelService _reporteService;

    public SesionesController(IReporteExcelService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpPost("iniciar")]
    public async Task<IActionResult> IniciarSesion([FromBody] IniciarSesionCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("finalizar")]
    public async Task<IActionResult> FinalizarSesion([FromBody] FinalizarSesionCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("sync-batch")]
    public async Task<IActionResult> SincronizarSesionesBatch([FromBody] SincronizarSesionesBatchCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("auditoria")]
    public async Task<IActionResult> GetSesionesAuditoria(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? aulaId,
        [FromQuery] string? emailEstudiante,
        [FromQuery] string? hostname,
        [FromQuery] TipoCierreSesion? tipoCierre,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanioPagina = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSesionesAuditoriaQuery(fechaInicio, fechaFin, aulaId, emailEstudiante, hostname, tipoCierre, pagina, tamanioPagina);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("exportar/excel")]
    public async Task<IActionResult> ExportarExcel(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? aulaId,
        [FromQuery] string? emailEstudiante,
        [FromQuery] string? hostname,
        [FromQuery] TipoCierreSesion? tipoCierre,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSesionesAuditoriaQuery(fechaInicio, fechaFin, aulaId, emailEstudiante, hostname, tipoCierre, 1, 100000);
        var result = await Mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        var bytes = _reporteService.GenerarReporteAuditoriaExcel(result.Value.Sesiones);
        var nombreArchivo = $"Auditoria_Laboratorios_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
    }

    [HttpGet("exportar/csv")]
    public async Task<IActionResult> ExportarCsv(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? aulaId,
        [FromQuery] string? emailEstudiante,
        [FromQuery] string? hostname,
        [FromQuery] TipoCierreSesion? tipoCierre,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSesionesAuditoriaQuery(fechaInicio, fechaFin, aulaId, emailEstudiante, hostname, tipoCierre, 1, 100000);
        var result = await Mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        var bytes = _reporteService.GenerarReporteAuditoriaCsv(result.Value.Sesiones);
        var nombreArchivo = $"Auditoria_Laboratorios_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
    }
}
