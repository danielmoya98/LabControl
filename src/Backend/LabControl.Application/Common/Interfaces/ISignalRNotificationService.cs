using LabControl.Domain.Enums;

namespace LabControl.Application.Common.Interfaces;

public interface ISignalRNotificationService
{
    Task NotifyEstadoComputadoraCambiadoAsync(int computadoraId, string hostname, EstadoComputadora nuevoEstado, string? emailEstudiante = null, CancellationToken cancellationToken = default);
    Task SendAlertaTerminalAsync(string targetHostname, string mensaje, CancellationToken cancellationToken = default);
    Task SendComandoCierreSesionAsync(string targetHostname, TipoCierreSesion motivo, CancellationToken cancellationToken = default);
}
