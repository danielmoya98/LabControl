using System;
using System.Collections.Generic;

namespace LabControl.Application.Common.Interfaces;

public record AsistenciaEstudianteItemDto(
    int NumeroPuesto,
    string Hostname,
    string EmailEstudiante,
    DateTime FechaHoraInicio,
    DateTime? FechaHoraFin,
    int? DuracionMinutos,
    string TipoCierre
);

public record ReporteAsistenciaClaseDto(
    string SedeNombre,
    string AulaNombre,
    string? MateriaNombre,
    string? MateriaSigla,
    string? DocenteNombre,
    string? DocenteEmail,
    string? GrupoParalelo,
    DateTime Fecha,
    TimeSpan HoraInicioProgramada,
    TimeSpan HoraFinProgramada,
    int CapacidadAula,
    List<AsistenciaEstudianteItemDto> Estudiantes
);

public record ReporteAuditoriaPdfDto(
    string SedeNombre,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    string? FiltrosTexto,
    int TotalRegistros,
    double TotalHorasUso,
    int TotalEstudiantesUnicos,
    double PromedioMinutosPorSesion,
    List<SesionAuditoriaDto> Sesiones
);

public record InfractorItemDto(
    string EmailEstudiante,
    string? NombreEstudiante,
    int IncidentesCount,
    double TotalHorasDesperdiciadas
);

public record ReporteEnergiaPdfDto(
    string SedeNombre,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    double TotalHorasDesperdiciadas,
    int TotalIncidentes,
    int TotalEquiposAfectados,
    List<RegistroEnergiaExportDto> Incidentes,
    List<InfractorItemDto> TopInfractores
);

public interface IReportePdfService
{
    byte[] GenerarReporteAsistenciaClase(ReporteAsistenciaClaseDto datos);
    byte[] GenerarReporteAuditoria(ReporteAuditoriaPdfDto datos);
    byte[] GenerarReporteEnergia(ReporteEnergiaPdfDto datos);
}
