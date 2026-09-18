using Microsoft.AspNetCore.SignalR;
using LabControl.Api.Hubs;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Enums;

namespace LabControl.Api.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<LaboratorioHub, ILaboratorioClient> _hubContext;

    public SignalRNotificationService(IHubContext<LaboratorioHub, ILaboratorioClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyEstadoComputadoraCambiadoAsync(
        int computadoraId,
        string hostname,
        EstadoComputadora nuevoEstado,
        string? emailEstudiante = null,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("PanelesAdmin")
            .RecibirEstadoComputadora(computadoraId, hostname, nuevoEstado, emailEstudiante);
    }

    public async Task SendAlertaTerminalAsync(string targetHostname, string mensaje, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Terminal_{targetHostname.ToUpperInvariant()}")
            .RecibirAlertaTerminal(mensaje);
    }

    public async Task SendAlertaAulaAsync(int aulaId, string mensaje, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Aula_{aulaId}")
            .RecibirAlertaTerminal(mensaje);
    }

    public async Task SendAlertaGlobalAsync(string mensaje, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("TodasLasTerminales")
            .RecibirAlertaTerminal(mensaje);
    }

    public async Task SendComandoCierreSesionAsync(string targetHostname, TipoCierreSesion motivo, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Terminal_{targetHostname.ToUpperInvariant()}")
            .RecibirComandoCierreSesion(motivo);
    }

    public async Task SendComandoCierreSesionAulaAsync(int aulaId, TipoCierreSesion motivo, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Aula_{aulaId}")
            .RecibirComandoCierreSesion(motivo);
    }

    public async Task SendComandoCierreSesionGlobalAsync(TipoCierreSesion motivo, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("TodasLasTerminales")
            .RecibirComandoCierreSesion(motivo);
    }
}
