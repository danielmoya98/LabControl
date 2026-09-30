using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Energia.Commands.EvaluarEquiposEncendidos;
using LabControl.Application.Features.Energia.Queries.GetReporteEnergia;

namespace LabControl.Api.Controllers;

public class EnergiaController : ApiControllerBase
{
    private readonly IReporteExcelService _reporteService;

    public EnergiaController(IReporteExcelService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet("reporte")]
    public async Task<IActionResult> GetReporte(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? aulaId,
        [FromQuery] string? emailEstudiante,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetReporteEnergiaQuery(fechaInicio, fechaFin, aulaId, emailEstudiante), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("evaluar")]
    public async Task<IActionResult> EvaluarEquipos(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new EvaluarEquiposEncendidosCommand(), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("exportar/excel")]
    public async Task<IActionResult> ExportarExcel(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? aulaId,
        [FromQuery] string? emailEstudiante,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetReporteEnergiaQuery(fechaInicio, fechaFin, aulaId, emailEstudiante), cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error.Message });

        var incidentesExport = result.Value.Incidentes.Select(i => new RegistroEnergiaExportDto(
            i.Id,
            i.AulaNombre,
            i.Hostname,
            i.UltimoEstudianteEmail,
            i.UltimoEstudianteNombre,
            i.FechaDeteccionUtc,
            i.HorasInactivaEncendida,
            i.MotivoInfraccion
        )).ToList();

        var bytes = _reporteService.GenerarReporteEnergiaExcel(incidentesExport);
        var nombreArchivo = $"Auditoria_Energia_LabControl_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
    }

    [HttpGet("exportar/csv")]
    public async Task<IActionResult> ExportarCsv(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? aulaId,
        [FromQuery] string? emailEstudiante,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetReporteEnergiaQuery(fechaInicio, fechaFin, aulaId, emailEstudiante), cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error.Message });

        var incidentesExport = result.Value.Incidentes.Select(i => new RegistroEnergiaExportDto(
            i.Id,
            i.AulaNombre,
            i.Hostname,
            i.UltimoEstudianteEmail,
            i.UltimoEstudianteNombre,
            i.FechaDeteccionUtc,
            i.HorasInactivaEncendida,
            i.MotivoInfraccion
        )).ToList();

        var bytes = _reporteService.GenerarReporteEnergiaCsv(incidentesExport);
        var nombreArchivo = $"Auditoria_Energia_LabControl_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
    }

    [HttpGet("exportar/pdf")]
    public async Task<IActionResult> ExportarPdf(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? aulaId,
        [FromQuery] string? emailEstudiante,
        [FromServices] IReportePdfService pdfService,
        [FromServices] IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetReporteEnergiaQuery(fechaInicio, fechaFin, aulaId, emailEstudiante), cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error.Message });

        var incidentesExport = result.Value.Incidentes.Select(i => new RegistroEnergiaExportDto(
            i.Id,
            i.AulaNombre,
            i.Hostname,
            i.UltimoEstudianteEmail,
            i.UltimoEstudianteNombre,
            i.FechaDeteccionUtc,
            i.HorasInactivaEncendida,
            i.MotivoInfraccion
        )).ToList();

        var topInfractores = result.Value.TopInfractores.Select(t => new InfractorItemDto(
            t.EstudianteEmail,
            t.EstudianteNombre,
            t.CantidadIncidentes,
            t.TotalHorasDesperdiciadas
        )).ToList();

        var sede = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.Sedes, cancellationToken);
        string sedeNombre = sede?.Nombre ?? "Universidad del Valle - Sede Sucre";

        var pdfDto = new ReporteEnergiaPdfDto(
            sedeNombre,
            fechaInicio,
            fechaFin,
            result.Value.TotalHorasDesperdiciadas,
            result.Value.TotalIncidentes,
            result.Value.TotalEquiposAfectados,
            incidentesExport,
            topInfractores
        );

        var bytes = pdfService.GenerarReporteEnergia(pdfDto);
        var nombreArchivo = $"Auditoria_Energia_LabControl_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return File(bytes, "application/pdf", nombreArchivo);
    }

    [HttpGet("configuracion-apagado")]
    public IActionResult GetConfiguracionApagado([FromServices] LabControl.Api.Services.CampusShutdownManager shutdownManager)
    {
        var config = shutdownManager.GetConfiguracion();
        return Ok(config);
    }

    [HttpPost("configuracion-apagado")]
    public IActionResult GuardarConfiguracionApagado(
        [FromBody] LabControl.Application.Features.Energia.ConfiguracionApagadoCampusDto request,
        [FromServices] LabControl.Api.Services.CampusShutdownManager shutdownManager)
    {
        shutdownManager.SaveConfiguracion(request);
        return Ok(new { message = "Configuración de apagado masivo del campus guardada exitosamente." });
    }

    [HttpPost("apagar-todo-campus")]
    public async Task<IActionResult> ApagarTodoCampus(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] LabControl.Api.Controllers.ComandoEnergiaRequest? request,
        [FromServices] LabControl.Api.Services.CampusShutdownManager shutdownManager,
        [FromServices] IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken)
    {
        var motivo = string.IsNullOrWhiteSpace(request?.Motivo)
            ? "Apagado manual masivo de todo el campus solicitado desde el WebAdmin"
            : request.Motivo;

        int totalAfectados = await shutdownManager.EjecutarApagadoCampusAsync(scopeFactory, motivo, cancellationToken);
        return Ok(new { message = $"Orden de apagado masivo transmitida con éxito a {totalAfectados} computadoras del campus." });
    }
}
