using LabControl.Domain.Enums;

namespace LabControl.Application.Common.Interfaces;

public interface ISignalRNotificationService
{
    Task NotifyEstadoComputadoraCambiadoAsync(int computadoraId, string hostname, EstadoComputadora nuevoEstado, string? emailEstudiante = null, CancellationToken cancellationToken = default);
    Task SendAlertaTerminalAsync(string targetHostname, string mensaje, CancellationToken cancellationToken = default);
    Task SendAlertaAulaAsync(int aulaId, string mensaje, CancellationToken cancellationToken = default);
    Task SendAlertaGlobalAsync(string mensaje, CancellationToken cancellationToken = default);
    Task SendComandoCierreSesionAsync(string targetHostname, TipoCierreSesion motivo, CancellationToken cancellationToken = default);
    Task SendComandoCierreSesionAulaAsync(int aulaId, TipoCierreSesion motivo, CancellationToken cancellationToken = default);
    Task SendComandoCierreSesionGlobalAsync(TipoCierreSesion motivo, CancellationToken cancellationToken = default);
    Task SendComandoEnergiaTerminalAsync(string targetHostname, string tipoComando, string motivo, CancellationToken cancellationToken = default);
    Task SendComandoEnergiaAulaAsync(int aulaId, string tipoComando, string motivo, CancellationToken cancellationToken = default);
    Task SendActualizacionPoliticaAulaAsync(int aulaId, string aulaNombre, int minutosInactividad, int accionInactividad, CancellationToken cancellationToken = default);
    Task SendSolicitudHeartbeatGlobalAsync(CancellationToken cancellationToken = default);
    Task SendSolicitudHeartbeatAulaAsync(int aulaId, CancellationToken cancellationToken = default);
    Task SendSolicitudHeartbeatTerminalAsync(string targetHostname, CancellationToken cancellationToken = default);
}
