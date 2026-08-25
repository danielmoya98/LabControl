using System.Net.Http;
using System.Net.Http.Json;

namespace LabControl.Client.Kiosk.Services;

public class AulaInfoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int Capacidad { get; set; }
    public string? Pabellon { get; set; }
}

public class AutoRegistroRequest
{
    public int AulaId { get; set; }
    public string Hostname { get; set; } = "";
    public string IpActual { get; set; } = "";
    public string MacAddress { get; set; } = "";
}

public class AutoRegistroResponse
{
    public int ComputadoraId { get; set; }
    public int AulaId { get; set; }
    public string Hostname { get; set; } = "";
    public string IpActual { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string EstadoActual { get; set; } = "";
}

public class KioskApiService
{
    private readonly HttpClient _httpClient;

    public KioskApiService(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public async Task<List<AulaInfoDto>> GetAulasDisponiblesAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<AulaInfoDto>>("api/aulas");
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<AutoRegistroResponse?> AutoRegistrarAsync(int aulaId, string hostname, string ipActual, string macAddress)
    {
        try
        {
            var req = new AutoRegistroRequest
            {
                AulaId = aulaId,
                Hostname = hostname,
                IpActual = ipActual,
                MacAddress = macAddress
            };

            var response = await _httpClient.PostAsJsonAsync("api/computadoras/auto-registro", req);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AutoRegistroResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
