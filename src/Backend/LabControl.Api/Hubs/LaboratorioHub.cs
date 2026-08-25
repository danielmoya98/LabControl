using Microsoft.AspNetCore.SignalR;
using LabControl.Domain.Enums;

namespace LabControl.Api.Hubs;

public class LaboratorioHub : Hub<ILaboratorioClient>
{
    public async Task RegistrarTerminal(string hostname, string macAddress)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Terminal_{hostname.ToUpperInvariant()}");
        await Groups.AddToGroupAsync(Context.ConnectionId, "TodasLasTerminales");
    }

    public async Task RegistrarPanelWebAdmin()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "PanelesAdmin");
    }
}
