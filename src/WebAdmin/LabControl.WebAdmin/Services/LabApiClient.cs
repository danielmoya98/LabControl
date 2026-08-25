using System.Net.Http.Json;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
using LabControl.Application.Features.Auth.Commands.LoginAdmin;
using LabControl.Application.Features.Computadoras.Commands.CreateComputadora;
using LabControl.Application.Features.Computadoras.Queries.GetEstadoAulasMapa;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
using LabControl.Domain.Enums;

namespace LabControl.WebAdmin.Services;

public class LabApiClient
{
    private readonly HttpClient _httpClient;

    public LabApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginAdminResponseDto?> LoginAdminAsync(string email, string password)
    {
        try
        {
            var command = new LoginAdminCommand(email, password);
            var response = await _httpClient.PostAsJsonAsync("api/auth/login-admin", command);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginAdminResponseDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<AulaEstadoDto>> GetMapaAulasAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<AulaEstadoDto>>("api/computadoras/mapa-aulas");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<AulaDto>> GetAulasAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<AulaDto>>("api/aulas");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> CreateAulaAsync(string nombre, int capacidad, string? pabellon)
    {
        try
        {
            var command = new CreateAulaCommand(nombre, capacidad, pabellon);
            var response = await _httpClient.PostAsJsonAsync("api/aulas", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateComputadoraAsync(int aulaId, string hostname, string ipActual, string macAddress)
    {
        try
        {
            var command = new CreateComputadoraCommand(aulaId, hostname, ipActual, macAddress);
            var response = await _httpClient.PostAsJsonAsync("api/computadoras", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<BloqueHorarioDto>> GetHorariosByAulaAsync(int aulaId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<BloqueHorarioDto>>($"api/horarios/{aulaId}");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> CreateBloqueHorarioAsync(int aulaId, DiaSemana diaSemana, TimeSpan horaInicio, TimeSpan horaFin, bool esRecreo, string? descripcion)
    {
        try
        {
            var command = new CreateBloqueHorarioCommand(aulaId, diaSemana, horaInicio, horaFin, esRecreo, descripcion);
            var response = await _httpClient.PostAsJsonAsync("api/horarios", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
