using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Entities;
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
        var hostNorm = (hostname ?? "").Trim().ToUpperInvariant();
        var macNorm = (macAddress ?? "").Trim().ToUpperInvariant();

        if (!string.IsNullOrEmpty(hostNorm))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Terminal_{hostNorm}");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, "TodasLasTerminales");

        int? aulaEfectivaId = null;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var signalR = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();

            var pc = await context.Computadoras
                .FirstOrDefaultAsync(c => c.Hostname == hostNorm || (!string.IsNullOrEmpty(macNorm) && c.MacAddress == macNorm));

            if (pc != null && pc.AulaId > 0)
            {
                aulaEfectivaId = pc.AulaId;
            }
            else
            {
                if (aulaId.HasValue && aulaId.Value > 0)
                {
                    aulaEfectivaId = aulaId.Value;
                }
                else
                {
                    var todasAulas = await context.Aulas.ToListAsync();
                    var coincidente = todasAulas.FirstOrDefault(a => 
                        !string.IsNullOrWhiteSpace(a.Nombre) && hostNorm.Contains(System.Text.RegularExpressions.Regex.Match(a.Nombre, @"\d+").Value));
                    aulaEfectivaId = coincidente?.Id ?? todasAulas.FirstOrDefault()?.Id ?? 1;
                }

                if (pc == null && !string.IsNullOrEmpty(hostNorm))
                {
                    var ipRes = IpAddress.Create("127.0.0.1");
                    var macRes = MacAddress.Create(!string.IsNullOrEmpty(macNorm) ? macNorm : "00:00:00:00:00:00");
                    if (ipRes.IsSuccess && macRes.IsSuccess)
                    {
                        var pcRes = Computadora.Create(aulaEfectivaId.Value, hostNorm, ipRes.Value, macRes.Value);
                        if (pcRes.IsSuccess)
                        {
                            pc = pcRes.Value;
                            context.Computadoras.Add(pc);
                            await context.SaveChangesAsync();
                            await signalR.NotifyEstadoComputadoraCambiadoAsync(pc.Id, pc.Hostname, pc.EstadoActual, null);
                        }
                    }
                }
            }

            if (aulaEfectivaId.HasValue && aulaEfectivaId.Value > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Aula_{aulaEfectivaId.Value}");
                var aula = await context.Aulas.FindAsync([aulaEfectivaId.Value]);
                if (aula != null)
                {
                    await Clients.Caller.RecibirActualizacionPoliticaAula(aula.Id, aula.Nombre, aula.MinutosInactividadMaximo, (int)aula.AccionInactividad);
                }
            }

            if (aulaId.HasValue && aulaId.Value > 0 && aulaId.Value != aulaEfectivaId)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Aula_{aulaId.Value}");
            }
        }
        catch
        {
            // Tolerancia a fallos en consulta inicial
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

        var hostNorm = hostname.Trim().ToUpperInvariant();
        var pc = await context.Computadoras
            .FirstOrDefaultAsync(c => c.Hostname == hostNorm);

        if (pc == null)
        {
            var todasAulas = await context.Aulas.ToListAsync();
            var coincidente = todasAulas.FirstOrDefault(a => 
                !string.IsNullOrWhiteSpace(a.Nombre) && hostNorm.Contains(System.Text.RegularExpressions.Regex.Match(a.Nombre, @"\d+").Value));
            int aulaIdEfectiva = coincidente?.Id ?? todasAulas.FirstOrDefault()?.Id ?? 1;

            var ipRes = IpAddress.Create(ip);
            var macRes = MacAddress.Create(!string.IsNullOrWhiteSpace(macAddress) ? macAddress : "00:00:00:00:00:00");
            if (ipRes.IsSuccess && macRes.IsSuccess)
            {
                var pcRes = Computadora.Create(aulaIdEfectiva, hostNorm, ipRes.Value, macRes.Value);
                if (pcRes.IsSuccess)
                {
                    pc = pcRes.Value;
                    context.Computadoras.Add(pc);
                    await context.SaveChangesAsync();
                }
            }
        }

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

    public async Task EnviarMiniaturaPantalla(string hostname, string imagenBase64)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var pc = await context.Computadoras.FirstOrDefaultAsync(c => c.Hostname.ToLower() == hostname.ToLower());
        int pcId = pc?.Id ?? 0;

        await Clients.Group("PanelesAdmin").RecibirMiniaturaPantalla(pcId, hostname, imagenBase64);
    }
}
