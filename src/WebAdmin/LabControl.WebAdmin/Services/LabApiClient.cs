using System.Net.Http.Json;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
using LabControl.Application.Features.Auth.Commands.LoginAdmin;
using LabControl.Application.Features.Computadoras.Commands.CreateComputadora;
using LabControl.Application.Features.Computadoras.Queries.GetEstadoAulasMapa;
using LabControl.Application.Features.Computadoras.Queries.GetComputadorasByAula;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
using LabControl.Application.Features.Sesiones.Queries.GetSesionesAuditoria;
using LabControl.Application.Common.Interfaces;
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

    // ==================== AULAS (UPDATE & DELETE) ====================
    public async Task<bool> UpdateAulaAsync(int id, string nombre, int capacidad, string? pabellon, bool activo = true)
    {
        try
        {
            var command = new LabControl.Application.Features.Aulas.Commands.UpdateAula.UpdateAulaCommand(id, nombre, capacidad, pabellon, activo);
            var response = await _httpClient.PutAsJsonAsync($"api/aulas/{id}", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAulaAsync(int id, bool softDelete = true)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/aulas/{id}?softDelete={softDelete}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== COMPUTADORAS (GET BY AULA, UPDATE & DELETE) ====================
    public async Task<List<ComputadoraDetalleDto>> GetComputadorasByAulaAsync(int aulaId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ComputadoraDetalleDto>>($"api/computadoras/aula/{aulaId}");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> UpdateComputadoraAsync(int id, int aulaId, string hostname, string ipActual, string macAddress, EstadoComputadora? estadoActual = null)
    {
        try
        {
            var command = new LabControl.Application.Features.Computadoras.Commands.UpdateComputadora.UpdateComputadoraCommand(
                id, aulaId, hostname, ipActual, macAddress, estadoActual);
            var response = await _httpClient.PutAsJsonAsync($"api/computadoras/{id}", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteComputadoraAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/computadoras/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== HORARIOS (UPDATE & DELETE) ====================
    public async Task<bool> UpdateBloqueHorarioAsync(int id, int aulaId, DiaSemana diaSemana, TimeSpan horaInicio, TimeSpan horaFin, bool esRecreo, string? descripcion)
    {
        try
        {
            var command = new LabControl.Application.Features.Horarios.Commands.UpdateBloqueHorario.UpdateBloqueHorarioCommand(
                id, aulaId, diaSemana, horaInicio, horaFin, esRecreo, descripcion);
            var response = await _httpClient.PutAsJsonAsync($"api/horarios/{id}", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteBloqueHorarioAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/horarios/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== CONTROL REMOTO & SIGNALR ====================
    public async Task<bool> DesloguearTerminalAsync(string? hostname = null, int? aulaId = null, TipoCierreSesion motivo = TipoCierreSesion.AdminRemoto)
    {
        try
        {
            var payload = new { Hostname = hostname, AulaId = aulaId, Motivo = motivo };
            var response = await _httpClient.PostAsJsonAsync("api/control/desloguear", payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> EnviarMensajeAlertaAsync(string mensaje, string? hostname = null, int? aulaId = null)
    {
        try
        {
            var payload = new { Mensaje = mensaje, Hostname = hostname, AulaId = aulaId };
            var response = await _httpClient.PostAsJsonAsync("api/control/enviar-mensaje", payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== AUDITORÍA Y REPORTES ====================
    public async Task<ResultadoAuditoriaDto?> GetAuditoriaSesionesAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? aulaId = null,
        string? emailEstudiante = null,
        string? hostname = null,
        TipoCierreSesion? tipoCierre = null,
        int pagina = 1,
        int tamanioPagina = 50)
    {
        try
        {
            var query = BuildAuditoriaQueryString(fechaInicio, fechaFin, aulaId, emailEstudiante, hostname, tipoCierre, pagina, tamanioPagina);
            return await _httpClient.GetFromJsonAsync<ResultadoAuditoriaDto>($"api/sesiones/auditoria{query}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> EnviarComandoEnergiaAsync(int computadoraId, string tipoComando, string? motivo = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/computadoras/{computadoraId}/energia", new { TipoComando = tipoComando, Motivo = motivo });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ApagarAulaCompletaAsync(int aulaId, string? motivo = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/aulas/{aulaId}/apagar-todo", new { Motivo = motivo });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public string GetExportarExcelUrl(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? aulaId = null,
        string? emailEstudiante = null,
        string? hostname = null,
        TipoCierreSesion? tipoCierre = null)
    {
        var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5256";
        var query = BuildAuditoriaQueryString(fechaInicio, fechaFin, aulaId, emailEstudiante, hostname, tipoCierre, null, null);
        return $"{baseUri}/api/sesiones/exportar/excel{query}";
    }

    public string GetExportarCsvUrl(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? aulaId = null,
        string? emailEstudiante = null,
        string? hostname = null,
        TipoCierreSesion? tipoCierre = null)
    {
        var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5256";
        var query = BuildAuditoriaQueryString(fechaInicio, fechaFin, aulaId, emailEstudiante, hostname, tipoCierre, null, null);
        return $"{baseUri}/api/sesiones/exportar/csv{query}";
    }

    private static string BuildAuditoriaQueryString(
        DateTime? fechaInicio,
        DateTime? fechaFin,
        int? aulaId,
        string? emailEstudiante,
        string? hostname,
        TipoCierreSesion? tipoCierre,
        int? pagina,
        int? tamanioPagina)
    {
        var parameters = new List<string>();

        if (fechaInicio.HasValue)
            parameters.Add($"fechaInicio={Uri.EscapeDataString(fechaInicio.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");
        if (fechaFin.HasValue)
            parameters.Add($"fechaFin={Uri.EscapeDataString(fechaFin.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");
        if (aulaId.HasValue && aulaId.Value > 0)
            parameters.Add($"aulaId={aulaId.Value}");
        if (!string.IsNullOrWhiteSpace(emailEstudiante))
            parameters.Add($"emailEstudiante={Uri.EscapeDataString(emailEstudiante.Trim())}");
        if (!string.IsNullOrWhiteSpace(hostname))
            parameters.Add($"hostname={Uri.EscapeDataString(hostname.Trim())}");
        if (tipoCierre.HasValue)
            parameters.Add($"tipoCierre={tipoCierre.Value}");
        if (pagina.HasValue)
            parameters.Add($"pagina={pagina.Value}");
        if (tamanioPagina.HasValue)
            parameters.Add($"tamanioPagina={tamanioPagina.Value}");

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
    }

    public async Task<ReporteEnergiaModel?> GetReporteEnergiaAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? aulaId = null,
        string? emailEstudiante = null)
    {
        try
        {
            var query = BuildEnergiaQueryString(fechaInicio, fechaFin, aulaId, emailEstudiante);
            return await _httpClient.GetFromJsonAsync<ReporteEnergiaModel>($"api/energia/reporte{query}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> EvaluarEquiposEnergiaAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/energia/evaluar", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public string GetExportarEnergiaExcelUrl(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? aulaId = null,
        string? emailEstudiante = null)
    {
        var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5256";
        var query = BuildEnergiaQueryString(fechaInicio, fechaFin, aulaId, emailEstudiante);
        return $"{baseUri}/api/energia/exportar/excel{query}";
    }

    public string GetExportarEnergiaCsvUrl(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? aulaId = null,
        string? emailEstudiante = null)
    {
        var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5256";
        var query = BuildEnergiaQueryString(fechaInicio, fechaFin, aulaId, emailEstudiante);
        return $"{baseUri}/api/energia/exportar/csv{query}";
    }

    private static string BuildEnergiaQueryString(
        DateTime? fechaInicio,
        DateTime? fechaFin,
        int? aulaId,
        string? emailEstudiante)
    {
        var parameters = new List<string>();

        if (fechaInicio.HasValue)
            parameters.Add($"fechaInicio={Uri.EscapeDataString(fechaInicio.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");
        if (fechaFin.HasValue)
            parameters.Add($"fechaFin={Uri.EscapeDataString(fechaFin.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");
        if (aulaId.HasValue && aulaId.Value > 0)
            parameters.Add($"aulaId={aulaId.Value}");
        if (!string.IsNullOrWhiteSpace(emailEstudiante))
            parameters.Add($"emailEstudiante={Uri.EscapeDataString(emailEstudiante.Trim())}");

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
    }
}

public class ReporteEnergiaModel
{
    public double TotalHorasDesperdiciadas { get; set; }
    public int TotalIncidentes { get; set; }
    public int TotalEquiposAfectados { get; set; }
    public List<RankingInfractorModel> TopInfractores { get; set; } = [];
    public List<RegistroEnergiaDetalleModel> Incidentes { get; set; } = [];
}

public class RankingInfractorModel
{
    public string EstudianteEmail { get; set; } = "";
    public string? EstudianteNombre { get; set; }
    public int CantidadIncidentes { get; set; }
    public double TotalHorasDesperdiciadas { get; set; }
}

public class RegistroEnergiaDetalleModel
{
    public int Id { get; set; }
    public int ComputadoraId { get; set; }
    public string Hostname { get; set; } = "";
    public int AulaId { get; set; }
    public string AulaNombre { get; set; } = "";
    public string UltimoEstudianteEmail { get; set; } = "";
    public string? UltimoEstudianteNombre { get; set; }
    public DateTime FechaDeteccionUtc { get; set; }
    public double HorasInactivaEncendida { get; set; }
    public string MotivoInfraccion { get; set; } = "";
    public bool Resuelto { get; set; }
}
