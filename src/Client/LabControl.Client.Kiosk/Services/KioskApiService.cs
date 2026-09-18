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
    public string? CpuModelo { get; set; }
    public int? RamTotalGb { get; set; }
    public int? DiscoTotalGb { get; set; }
    public int? DiscoLibreGb { get; set; }
    public string? SistemaOperativo { get; set; }
    public double? UptimeHoras { get; set; }
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

public class IniciarSesionApiRequest
{
    public int? ComputadoraId { get; set; }
    public string? Hostname { get; set; }
    public string EmailEstudiante { get; set; } = "";
    public int MinutosLimite { get; set; } = 90;
}

public class IniciarSesionApiResponse
{
    public int SesionId { get; set; }
    public int ComputadoraId { get; set; }
    public string Hostname { get; set; } = "";
    public string EmailEstudiante { get; set; } = "";
    public DateTime FechaHoraInicio { get; set; }
    public int MinutosLimite { get; set; }
}

public class FinalizarSesionApiRequest
{
    public int? SesionId { get; set; }
    public int? ComputadoraId { get; set; }
    public string? Hostname { get; set; }
    public int TipoCierre { get; set; } = 1; // Manual
}

public class FinalizarSesionApiResponse
{
    public int SesionId { get; set; }
    public int DuracionMinutos { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public int TipoCierre { get; set; }
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

    public async Task<AutoRegistroResponse?> AutoRegistrarAsync(
        int aulaId,
        string hostname,
        string ipActual,
        string macAddress,
        HardwareInfoDto? hw = null)
    {
        try
        {
            hw ??= SystemInfoService.GetHardwareInfo();

            var req = new AutoRegistroRequest
            {
                AulaId = aulaId,
                Hostname = hostname,
                IpActual = ipActual,
                MacAddress = macAddress,
                CpuModelo = hw.CpuModelo,
                RamTotalGb = hw.RamTotalGb,
                DiscoTotalGb = hw.DiscoTotalGb,
                DiscoLibreGb = hw.DiscoLibreGb,
                SistemaOperativo = hw.SistemaOperativo,
                UptimeHoras = hw.UptimeHoras
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

    public async Task<IniciarSesionApiResponse?> IniciarSesionAsync(int? computadoraId, string? hostname, string emailEstudiante, int minutosLimite = 90)
    {
        try
        {
            var req = new IniciarSesionApiRequest
            {
                ComputadoraId = computadoraId,
                Hostname = hostname,
                EmailEstudiante = emailEstudiante,
                MinutosLimite = minutosLimite
            };

            var response = await _httpClient.PostAsJsonAsync("api/sesiones/iniciar", req);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IniciarSesionApiResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<FinalizarSesionApiResponse?> FinalizarSesionAsync(int? sesionId, int? computadoraId, string? hostname, int tipoCierre = 1)
    {
        try
        {
            var req = new FinalizarSesionApiRequest
            {
                SesionId = sesionId,
                ComputadoraId = computadoraId,
                Hostname = hostname,
                TipoCierre = tipoCierre
            };

            var response = await _httpClient.PostAsJsonAsync("api/sesiones/finalizar", req);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<FinalizarSesionApiResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SincronizarBatchAsync(string hostname, List<SesionBatchItemDto> sesiones)
    {
        try
        {
            var req = new SincronizarBatchRequest
            {
                Hostname = hostname,
                Sesiones = sesiones
            };

            var response = await _httpClient.PostAsJsonAsync("api/sesiones/sync-batch", req);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await _httpClient.GetAsync("api/aulas", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class SesionBatchItemDto
{
    public string EmailEstudiante { get; set; } = "";
    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraFin { get; set; }
    public int TipoCierre { get; set; }
}

public class SincronizarBatchRequest
{
    public string Hostname { get; set; } = "";
    public List<SesionBatchItemDto> Sesiones { get; set; } = new();
}
