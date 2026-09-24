using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Enums;

namespace LabControl.Infrastructure.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    public Task NotifyEstadoComputadoraCambiadoAsync(int computadoraId, string hostname, EstadoComputadora nuevoEstado, string? emailEstudiante = null, CancellationToken cancellationToken = default)
    {
        // Se conectará con LaboratorioHub en LabControl.Api
        return Task.CompletedTask;
    }

    public Task SendAlertaTerminalAsync(string targetHostname, string mensaje, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendAlertaAulaAsync(int aulaId, string mensaje, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendAlertaGlobalAsync(string mensaje, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendComandoCierreSesionAsync(string targetHostname, TipoCierreSesion motivo, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendComandoCierreSesionAulaAsync(int aulaId, TipoCierreSesion motivo, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendComandoCierreSesionGlobalAsync(TipoCierreSesion motivo, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendComandoEnergiaTerminalAsync(string targetHostname, string tipoComando, string motivo, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendComandoEnergiaAulaAsync(int aulaId, string tipoComando, string motivo, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendActualizacionPoliticaAulaAsync(int aulaId, string aulaNombre, int minutosInactividad, int accionInactividad, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendSolicitudHeartbeatGlobalAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendSolicitudHeartbeatAulaAsync(int aulaId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendSolicitudHeartbeatTerminalAsync(string targetHostname, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
