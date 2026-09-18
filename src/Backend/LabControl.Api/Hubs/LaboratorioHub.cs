using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Api.Hubs;

public class LaboratorioHub : Hub<ILaboratorioClient>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LaboratorioHub(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task RegistrarTerminal(string hostname, string macAddress, int? aulaId = null)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Terminal_{hostname.ToUpperInvariant()}");
        await Groups.AddToGroupAsync(Context.ConnectionId, "TodasLasTerminales");
        if (aulaId.HasValue && aulaId.Value > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Aula_{aulaId.Value}");
        }
    }

    public async Task RegistrarPanelWebAdmin()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "PanelesAdmin");
    }

    public async Task EnviarHeartbeat(string hostname, string macAddress, string ip)
    {
        if (string.IsNullOrWhiteSpace(hostname)) return;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var signalR = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();

        var pc = await context.Computadoras
            .FirstOrDefaultAsync(c => c.Hostname == hostname.Trim().ToUpperInvariant());

        if (pc != null)
        {
            var estabaOffline = pc.EstadoActual == EstadoComputadora.Offline;
            var ipResult = IpAddress.Create(ip);
            if (ipResult.IsSuccess)
            {
                pc.ActualizarHeartbeat(ipResult.Value);
            }

            string? emailEstudiante = null;
            if (estabaOffline)
            {
                var sesionActiva = await context.SesionesUso
                    .Where(s => s.ComputadoraId == pc.Id && s.FechaHoraFin == null)
                    .OrderByDescending(s => s.FechaHoraInicio)
                    .FirstOrDefaultAsync();

                if (sesionActiva != null)
                {
                    pc.CambiarEstado(EstadoComputadora.EnUso);
                    emailEstudiante = sesionActiva.EmailEstudiante;
                }
                else
                {
                    pc.CambiarEstado(EstadoComputadora.Disponible);
                }
            }

            await context.SaveChangesAsync();

            if (estabaOffline)
            {
                await signalR.NotifyEstadoComputadoraCambiadoAsync(pc.Id, pc.Hostname, pc.EstadoActual, emailEstudiante);
            }
        }
    }

    public async Task EnviarHeartbeatConTelemetria(string hostname, string macAddress, string ip, int cpuUso, int ramUso, int discoLibreGb)
    {
        await EnviarHeartbeat(hostname, macAddress, ip);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var pc = await context.Computadoras.FirstOrDefaultAsync(c => c.Hostname == hostname.Trim().ToUpperInvariant());
        if (pc != null)
        {
            if (pc.DiscoLibreGb != discoLibreGb)
            {
                pc.ActualizarEspecificacionesHardware(null, null, null, discoLibreGb, null, null);
                await context.SaveChangesAsync();
            }

            await Clients.Group("PanelesAdmin").RecibirTelemetria(pc.Id, pc.Hostname, cpuUso, ramUso, discoLibreGb);
        }
    }
}
