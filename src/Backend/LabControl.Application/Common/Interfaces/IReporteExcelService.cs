using LabControl.Domain.Enums;

namespace LabControl.Application.Common.Interfaces;

public record SesionAuditoriaDto(
    int SesionId,
    int ComputadoraId,
    string Hostname,
    int AulaId,
    string NombreAula,
    string EmailEstudiante,
    DateTime FechaHoraInicio,
    DateTime? FechaHoraFin,
    int? DuracionMinutos,
    TipoCierreSesion TipoCierre,
    SyncStatus SyncStatus,
    DateTime FechaSincronizacion
);

public record RegistroEnergiaExportDto(
    int Id,
    string AulaNombre,
    string Hostname,
    string UltimoEstudianteEmail,
    string? UltimoEstudianteNombre,
    DateTime FechaDeteccion,
    double HorasInactivaEncendida,
    string MotivoInfraccion
);

public interface IReporteExcelService
{
    byte[] GenerarReporteAuditoriaExcel(List<SesionAuditoriaDto> sesiones, string? subtituloFiltros = null);
    byte[] GenerarReporteAuditoriaCsv(List<SesionAuditoriaDto> sesiones);
    byte[] GenerarReporteEnergiaExcel(List<RegistroEnergiaExportDto> incidentes, string? subtituloFiltros = null);
    byte[] GenerarReporteEnergiaCsv(List<RegistroEnergiaExportDto> incidentes);
}
