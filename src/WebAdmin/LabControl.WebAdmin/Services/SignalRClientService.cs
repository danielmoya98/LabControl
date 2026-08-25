using Microsoft.AspNetCore.SignalR.Client;
using LabControl.Domain.Enums;

namespace LabControl.WebAdmin.Services;

public class SignalRClientService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public event Action<int, string, EstadoComputadora, string?>? OnEstadoComputadoraCambiado;

    public async Task StartAsync(string hubUrl)
    {
        if (_hubConnection != null) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<int, string, EstadoComputadora, string?>(
            "RecibirEstadoComputadora",
            (computadoraId, hostname, nuevoEstado, emailEstudiante) =>
            {
                OnEstadoComputadoraCambiado?.Invoke(computadoraId, hostname, nuevoEstado, emailEstudiante);
            });

        try
        {
            await _hubConnection.StartAsync();
            await _hubConnection.SendAsync("RegistrarPanelWebAdmin");
        }
        catch
        {
            // Silencioso en caso de reintento automático
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
