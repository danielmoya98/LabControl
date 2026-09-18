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

public interface IReporteExcelService
{
    byte[] GenerarReporteAuditoriaExcel(List<SesionAuditoriaDto> sesiones, string? subtituloFiltros = null);
    byte[] GenerarReporteAuditoriaCsv(List<SesionAuditoriaDto> sesiones);
}
