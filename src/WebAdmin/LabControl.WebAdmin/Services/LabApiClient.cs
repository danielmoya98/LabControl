using System.Net.Http.Json;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
using LabControl.Application.Features.Auth.Commands.LoginAdmin;
using LabControl.Application.Features.Computadoras.Commands.CreateComputadora;
using LabControl.Application.Features.Computadoras.Queries.GetEstadoAulasMapa;
using LabControl.Application.Features.Computadoras.Queries.GetComputadorasByAula;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
using LabControl.Application.Features.Sesiones.Queries.GetSesionesAuditoria;
using LabControl.Application.Features.Onboarding.Commands.CompleteOnboarding;
using LabControl.Application.Features.Onboarding.Queries.GetOnboardingStatus;
using LabControl.Application.Features.Bloques.Commands.CreateBloque;
using LabControl.Application.Features.Bloques.Queries.GetBloques;
using LabControl.Application.Features.Docentes.Commands.CreateDocente;
using LabControl.Application.Features.Docentes.Queries.GetDocentes;
using LabControl.Application.Features.Materias.Commands.CreateMateria;
using LabControl.Application.Features.Materias.Queries.GetMaterias;
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

    // ==================== ONBOARDING ====================
    public async Task<OnboardingStatusDto?> GetOnboardingStatusAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<OnboardingStatusDto>("api/onboarding/status");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, CompleteOnboardingResponseDto? Data, string? ErrorMessage)> CompleteOnboardingAsync(CompleteOnboardingCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/onboarding/complete", command);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CompleteOnboardingResponseDto>();
                return (true, data, null);
            }

            try
            {
                var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
                var errorMsg = problem?.Detail ?? problem?.Title ?? "Error al procesar el onboarding.";
                return (false, null, errorMsg);
            }
            catch
            {
                var raw = await response.Content.ReadAsStringAsync();
                return (false, null, !string.IsNullOrWhiteSpace(raw) ? raw : "Error en el servidor al procesar el onboarding.");
            }
        }
        catch (Exception ex)
        {
            return (false, null, $"Error de conexión con la API: {ex.Message}");
        }
    }

    // ==================== BLOQUES ====================
    public async Task<List<BloqueDto>> GetBloquesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<BloqueDto>>("api/bloques");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> CreateBloqueAsync(string nombre, string codigo, bool tienePisos, int? totalPisos, string? descripcion)
    {
        try
        {
            var command = new CreateBloqueCommand(nombre, codigo, tienePisos, totalPisos, descripcion);
            var response = await _httpClient.PostAsJsonAsync("api/bloques", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== DOCENTES ====================
    public async Task<List<DocenteDto>> GetDocentesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<DocenteDto>>("api/docentes");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> CreateDocenteAsync(string nombres, string apellidos, string email, string? telefono = null)
    {
        try
        {
            var command = new CreateDocenteCommand(nombres, apellidos, email, telefono);
            var response = await _httpClient.PostAsJsonAsync("api/docentes", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateDocenteAsync(int id, string nombres, string apellidos, string email, string? telefono, bool activo = true)
    {
        try
        {
            var payload = new { Id = id, Nombres = nombres, Apellidos = apellidos, EmailInstitucional = email, TelefonoContacto = telefono, Activo = activo };
            var response = await _httpClient.PutAsJsonAsync($"api/docentes/{id}", payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteDocenteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/docentes/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== MATERIAS ====================
    public async Task<List<MateriaDto>> GetMateriasAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<MateriaDto>>("api/materias");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> CreateMateriaAsync(string sigla, string nombre, string? carrera = null)
    {
        try
        {
            var command = new CreateMateriaCommand(sigla, nombre, carrera);
            var response = await _httpClient.PostAsJsonAsync("api/materias", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateMateriaAsync(int id, string sigla, string nombre, string? carrera, bool activo = true)
    {
        try
        {
            var payload = new { Id = id, Sigla = sigla, Nombre = nombre, Carrera = carrera, Activo = activo };
            var response = await _httpClient.PutAsJsonAsync($"api/materias/{id}", payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteMateriaAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/materias/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== USUARIOS DEL SISTEMA ====================
    public async Task<List<UsuarioSistemaDto>> GetUsuariosAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<UsuarioSistemaDto>>("api/usuarios");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<(bool Success, string? Error)> CreateUsuarioAsync(string email, string password, string nombreCompleto, string role)
    {
        try
        {
            var payload = new { Email = email, Password = password, NombreCompleto = nombreCompleto, Role = role };
            var response = await _httpClient.PostAsJsonAsync("api/usuarios", payload);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateUsuarioAsync(string id, string nombreCompleto, string role, bool activo, string? newPassword = null)
    {
        try
        {
            var payload = new { NombreCompleto = nombreCompleto, Role = role, Activo = activo, NewPassword = newPassword };
            var response = await _httpClient.PutAsJsonAsync($"api/usuarios/{id}", payload);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<bool> DeleteUsuarioAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/usuarios/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== DESEMPEÑO MENSUAL ====================
    public async Task<LabControl.Application.Features.Sesiones.Queries.GetDesempenoMensual.DesempenoMensualDto?> GetDesempenoMensualAsync(int meses = 12)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<LabControl.Application.Features.Sesiones.Queries.GetDesempenoMensual.DesempenoMensualDto>($"api/sesiones/desempeno-mensual?meses={meses}");
        }
        catch
        {
            return null;
        }
    }

    // ==================== AULAS ====================
    public async Task<List<AulaDto>> GetAulasAsync(int? bloqueId = null)
    {
        try
        {
            string url = bloqueId.HasValue ? $"api/aulas?bloqueId={bloqueId.Value}" : "api/aulas";
            var response = await _httpClient.GetFromJsonAsync<List<AulaDto>>(url);
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> CreateAulaAsync(
        string nombre, 
        int capacidad, 
        string? pabellon = null, 
        int minutosInactividad = 15, 
        TipoAccionInactividad accionInactividad = TipoAccionInactividad.ApagarEquipo,
        int? bloqueId = null,
        string? piso = null)
    {
        try
        {
            var command = new CreateAulaCommand(nombre, capacidad, pabellon, minutosInactividad, accionInactividad, bloqueId, piso);
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

    // ==================== PERIODOS ACADEMICOS / SEMESTRES ====================
    public async Task<List<PeriodoAcademicoModel>> GetPeriodosAcademicosAsync(bool soloActivos = false)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<PeriodoAcademicoModel>>($"api/periodos-academicos?soloActivos={soloActivos}");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CreatePeriodoAcademicoAsync(string nombre, DateTime fechaInicio, DateTime fechaFin, bool esActual = false)
    {
        try
        {
            var payload = new { 
                Nombre = nombre, 
                FechaInicio = DateTime.SpecifyKind(fechaInicio.Date, DateTimeKind.Unspecified), 
                FechaFin = DateTime.SpecifyKind(fechaFin.Date, DateTimeKind.Unspecified), 
                EsActual = esActual 
            };
            var response = await _httpClient.PostAsJsonAsync("api/periodos-academicos", payload);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var err = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al registrar el semestre." : err);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<bool> SetPeriodoAcademicoActualAsync(int id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/periodos-academicos/{id}/activar", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, int Copiados)> ClonarHorariosSemestreAsync(int periodoOrigenId, int periodoDestinoId)
    {
        try
        {
            var payload = new { PeriodoOrigenId = periodoOrigenId, PeriodoDestinoId = periodoDestinoId };
            var response = await _httpClient.PostAsJsonAsync("api/periodos-academicos/clonar", payload);
            if (response.IsSuccessStatusCode)
            {
                var count = await response.Content.ReadFromJsonAsync<int>();
                return (true, count);
            }
            return (false, 0);
        }
        catch
        {
            return (false, 0);
        }
    }

    // ==================== SEDES & SEGMENTO RED ====================
    public async Task<List<SedeDetalleModel>> GetSedesDetalleAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<SedeDetalleModel>>("api/sedes");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> UpdateSedeSegmentoRedAsync(int sedeId, string? segmentoRed)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/sedes/{sedeId}/segmento-red", new { SegmentoRed = segmentoRed });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ==================== HORARIOS ====================
    public async Task<List<BloqueHorarioDto>> GetHorariosByAulaAsync(int aulaId, int? periodoId = null)
    {
        try
        {
            var query = periodoId.HasValue ? $"?periodoId={periodoId.Value}" : "";
            var response = await _httpClient.GetFromJsonAsync<List<BloqueHorarioDto>>($"api/horarios/{aulaId}{query}");
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> CreateBloqueHorarioAsync(
        int aulaId, 
        DiaSemana diaSemana, 
        TimeSpan horaInicio, 
        TimeSpan horaFin, 
        bool esRecreo, 
        string? descripcion,
        int? materiaId = null,
        int? docenteId = null,
        string? grupoParalelo = null,
        bool esUsoLibre = false,
        int? periodoAcademicoId = null,
        string? docenteNombreManual = null,
        string? docenteEmailManual = null,
        string? materiaNombreManual = null)
    {
        try
        {
            var command = new CreateBloqueHorarioCommand(
                aulaId, diaSemana, horaInicio, horaFin, esRecreo, descripcion,
                materiaId, docenteId, grupoParalelo, esUsoLibre,
                periodoAcademicoId, docenteNombreManual, docenteEmailManual, materiaNombreManual);
            var response = await _httpClient.PostAsJsonAsync("api/horarios", command);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateBloqueHorarioAsync(
        int id,
        int aulaId,
        DiaSemana diaSemana,
        TimeSpan horaInicio,
        TimeSpan horaFin,
        bool esRecreo,
        string? descripcion,
        int? materiaId = null,
        int? docenteId = null,
        string? grupoParalelo = null,
        bool esUsoLibre = false,
        int? periodoAcademicoId = null,
        string? docenteNombreManual = null,
        string? docenteEmailManual = null,
        string? materiaNombreManual = null)
    {
        try
        {
            var command = new LabControl.Application.Features.Horarios.Commands.UpdateBloqueHorario.UpdateBloqueHorarioCommand(
                id, aulaId, diaSemana, horaInicio, horaFin, esRecreo, descripcion,
                materiaId, docenteId, grupoParalelo, esUsoLibre,
                periodoAcademicoId, docenteNombreManual, docenteEmailManual, materiaNombreManual);
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

    public string GetReporteAsistenciaPdfUrl(int bloqueId, DateTime? fecha = null)
    {
        var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5256";
        var query = fecha.HasValue ? $"?fecha={fecha.Value:yyyy-MM-dd}" : "";
        return $"{baseUri}/api/horarios/{bloqueId}/asistencia/pdf{query}";
    }

    // ==================== AULAS (UPDATE & DELETE) ====================
    public async Task<bool> UpdateAulaAsync(
        int id, 
        string nombre, 
        int capacidad, 
        string? pabellon = null, 
        bool activo = true, 
        int minutosInactividad = 15, 
        TipoAccionInactividad accionInactividad = TipoAccionInactividad.ApagarEquipo,
        int? bloqueId = null,
        string? piso = null)
    {
        try
        {
            var command = new LabControl.Application.Features.Aulas.Commands.UpdateAula.UpdateAulaCommand(
                id, nombre, capacidad, pabellon, activo, minutosInactividad, accionInactividad, bloqueId, piso);
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

    public string GetExportarPdfUrl(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? aulaId = null,
        string? emailEstudiante = null,
        string? hostname = null,
        TipoCierreSesion? tipoCierre = null)
    {
        var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5256";
        var query = BuildAuditoriaQueryString(fechaInicio, fechaFin, aulaId, emailEstudiante, hostname, tipoCierre, null, null);
        return $"{baseUri}/api/sesiones/exportar/pdf{query}";
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

    public string GetExportarEnergiaPdfUrl(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? aulaId = null,
        string? emailEstudiante = null)
    {
        var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5256";
        var query = BuildEnergiaQueryString(fechaInicio, fechaFin, aulaId, emailEstudiante);
        return $"{baseUri}/api/energia/exportar/pdf{query}";
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

public class UsuarioSistemaDto
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Role { get; set; } = "";
    public bool Activo { get; set; }
    public DateTime FechaRegistroUtc { get; set; }
}

public class PeriodoAcademicoModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool EsActual { get; set; }
    public bool Activo { get; set; }
    public int CantidadBloquesHorarios { get; set; }
}

public class SedeDetalleModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public string? Direccion { get; set; }
    public string? SegmentoRed { get; set; }
    public bool OnboardingCompletado { get; set; }
    public int TotalBloques { get; set; }
    public int TotalAulas { get; set; }
}
