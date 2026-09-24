using System.IO;
using Microsoft.Data.Sqlite;

namespace LabControl.Client.Kiosk.Services;

public class OfflineSesionItem
{
    public int Id { get; set; }
    public int? ComputadoraId { get; set; }
    public string Hostname { get; set; } = "";
    public string EmailEstudiante { get; set; } = "";
    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraFin { get; set; }
    public int? DuracionMinutos { get; set; }
    public int TipoCierre { get; set; } = 1;
    public string EstadoSync { get; set; } = "Pendiente";
    public int Intentos { get; set; }
}

/// <summary>
/// Motor de almacenamiento local SQLite para garantizar tolerancia a fallas de red
/// en terminales Kiosk, guardando sesiones iniciadas y cerradas en modo desconectado.
/// </summary>
public static class SqliteOfflineService
{
    private static readonly string DbPath = Path.Combine(LocalStorageService.GetDataDirectory(), "labcontrol_offline.db");
    private static readonly string ConnectionString = $"Data Source={DbPath}";
    private static bool _inicializado = false;
    private static readonly object _lock = new();

    public static async Task InicializarBaseDeDatosAsync()
    {
        if (_inicializado) return;

        try
        {
            var dir = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var tableCmd = @"
                CREATE TABLE IF NOT EXISTS OfflineSesionesQueue (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ComputadoraId INTEGER,
                    Hostname TEXT NOT NULL,
                    EmailEstudiante TEXT NOT NULL,
                    FechaHoraInicio TEXT NOT NULL,
                    FechaHoraFin TEXT,
                    DuracionMinutos INTEGER,
                    TipoCierre INTEGER NOT NULL,
                    EstadoSync TEXT NOT NULL DEFAULT 'Pendiente',
                    Intentos INTEGER NOT NULL DEFAULT 0,
                    FechaCreacion TEXT NOT NULL
                );";

            using var command = new SqliteCommand(tableCmd, connection);
            await command.ExecuteNonQueryAsync();

            _inicializado = true;
        }
        catch
        {
            // Falla de base de datos local no debe derribar la aplicación principal
        }
    }

    public static async Task<int> RegistrarInicioSesionOfflineAsync(int? computadoraId, string hostname, string emailEstudiante, DateTime inicio)
    {
        await InicializarBaseDeDatosAsync();

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var query = @"
            INSERT INTO OfflineSesionesQueue (ComputadoraId, Hostname, EmailEstudiante, FechaHoraInicio, TipoCierre, EstadoSync, Intentos, FechaCreacion)
            VALUES (@ComputadoraId, @Hostname, @EmailEstudiante, @FechaHoraInicio, 1, 'Pendiente', 0, @FechaCreacion);
            SELECT last_insert_rowid();";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@ComputadoraId", (object?)computadoraId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Hostname", hostname);
        command.Parameters.AddWithValue("@EmailEstudiante", emailEstudiante);
        command.Parameters.AddWithValue("@FechaHoraInicio", (inicio.Kind == DateTimeKind.Utc ? inicio : inicio.ToUniversalTime()).ToString("o"));
        command.Parameters.AddWithValue("@FechaCreacion", DateTime.UtcNow.ToString("o"));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public static async Task RegistrarFinSesionOfflineAsync(int sesionLocalId, DateTime fin, int tipoCierre)
    {
        await InicializarBaseDeDatosAsync();

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        // Obtener fecha de inicio para calcular duración
        DateTime? inicio = null;
        using (var selectCmd = new SqliteCommand("SELECT FechaHoraInicio FROM OfflineSesionesQueue WHERE Id = @Id", connection))
        {
            selectCmd.Parameters.AddWithValue("@Id", sesionLocalId);
            var obj = await selectCmd.ExecuteScalarAsync();
            if (obj != null && DateTime.TryParse(obj.ToString(), out var parsed))
            {
                inicio = parsed;
            }
        }

        int? duracionMinutos = null;
        if (inicio.HasValue)
        {
            duracionMinutos = (int)Math.Max(1, Math.Round((fin - inicio.Value).TotalMinutes));
        }

        var updateQuery = @"
            UPDATE OfflineSesionesQueue
            SET FechaHoraFin = @FechaHoraFin,
                DuracionMinutos = @DuracionMinutos,
                TipoCierre = @TipoCierre,
                EstadoSync = 'Pendiente'
            WHERE Id = @Id;";

        using var command = new SqliteCommand(updateQuery, connection);
        command.Parameters.AddWithValue("@Id", sesionLocalId);
        command.Parameters.AddWithValue("@FechaHoraFin", (fin.Kind == DateTimeKind.Utc ? fin : fin.ToUniversalTime()).ToString("o"));
        command.Parameters.AddWithValue("@DuracionMinutos", (object?)duracionMinutos ?? DBNull.Value);
        command.Parameters.AddWithValue("@TipoCierre", tipoCierre);

        await command.ExecuteNonQueryAsync();
    }

    public static async Task EncolarSesionCompletaOfflineAsync(int? computadoraId, string hostname, string emailEstudiante, DateTime inicio, DateTime fin, int tipoCierre)
    {
        await InicializarBaseDeDatosAsync();

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var duracion = (int)Math.Max(1, Math.Round((fin - inicio).TotalMinutes));

        var query = @"
            INSERT INTO OfflineSesionesQueue (ComputadoraId, Hostname, EmailEstudiante, FechaHoraInicio, FechaHoraFin, DuracionMinutos, TipoCierre, EstadoSync, Intentos, FechaCreacion)
            VALUES (@ComputadoraId, @Hostname, @EmailEstudiante, @FechaHoraInicio, @FechaHoraFin, @DuracionMinutos, @TipoCierre, 'Pendiente', 0, @FechaCreacion);";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@ComputadoraId", (object?)computadoraId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Hostname", hostname);
        command.Parameters.AddWithValue("@EmailEstudiante", emailEstudiante);
        command.Parameters.AddWithValue("@FechaHoraInicio", (inicio.Kind == DateTimeKind.Utc ? inicio : inicio.ToUniversalTime()).ToString("o"));
        command.Parameters.AddWithValue("@FechaHoraFin", (fin.Kind == DateTimeKind.Utc ? fin : fin.ToUniversalTime()).ToString("o"));
        command.Parameters.AddWithValue("@DuracionMinutos", duracion);
        command.Parameters.AddWithValue("@TipoCierre", tipoCierre);
        command.Parameters.AddWithValue("@FechaCreacion", DateTime.UtcNow.ToString("o"));

        await command.ExecuteNonQueryAsync();
    }

    public static async Task<List<OfflineSesionItem>> ObtenerSesionesPendientesAsync()
    {
        await InicializarBaseDeDatosAsync();

        var lista = new List<OfflineSesionItem>();

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        // Solo sincronizamos aquellas que ya tengan FechaHoraFin (sesiones concluidas)
        var query = @"
            SELECT Id, ComputadoraId, Hostname, EmailEstudiante, FechaHoraInicio, FechaHoraFin, DuracionMinutos, TipoCierre, EstadoSync, Intentos
            FROM OfflineSesionesQueue
            WHERE EstadoSync = 'Pendiente' AND FechaHoraFin IS NOT NULL
            ORDER BY Id ASC
            LIMIT 50;";

        using var command = new SqliteCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new OfflineSesionItem
            {
                Id = reader.GetInt32(0),
                ComputadoraId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Hostname = reader.GetString(2),
                EmailEstudiante = reader.GetString(3),
                FechaHoraInicio = DateTime.SpecifyKind(DateTime.Parse(reader.GetString(4)), DateTimeKind.Utc),
                FechaHoraFin = reader.IsDBNull(5) ? null : DateTime.SpecifyKind(DateTime.Parse(reader.GetString(5)), DateTimeKind.Utc),
                DuracionMinutos = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                TipoCierre = reader.GetInt32(7),
                EstadoSync = reader.GetString(8),
                Intentos = reader.GetInt32(9)
            };
            lista.Add(item);
        }

        return lista;
    }

    public static async Task MarcarSesionSincronizadaAsync(int id)
    {
        await InicializarBaseDeDatosAsync();

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var query = "UPDATE OfflineSesionesQueue SET EstadoSync = 'Sincronizado' WHERE Id = @Id;";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync();
    }

    public static async Task IncrementarIntentoAsync(int id)
    {
        await InicializarBaseDeDatosAsync();

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var query = "UPDATE OfflineSesionesQueue SET Intentos = Intentos + 1 WHERE Id = @Id;";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Al arrancar la aplicación, verifica si la sesión previa no tuvo un cierre formal
    /// (ej: el estudiante apagó la PC con el botón físico o hubo un corte de energía).
    /// Si existe, la cierra marcando tipo 7 (ApagadoForzado) para que se sincronice con el servidor central.
    /// </summary>
    public static async Task RecuperarSesionesHuerfanasAsync()
    {
        await InicializarBaseDeDatosAsync();

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var selectQuery = "SELECT Id, FechaHoraInicio FROM OfflineSesionesQueue WHERE FechaHoraFin IS NULL;";
        var huerfanas = new List<(int Id, DateTime Inicio)>();

        using (var cmd = new SqliteCommand(selectQuery, connection))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                if (DateTime.TryParse(reader.GetString(1), out var inicio))
                {
                    huerfanas.Add((id, inicio));
                }
            }
        }

        foreach (var (id, inicio) in huerfanas)
        {
            // Se asume como fin 5 minutos tras el inicio o hasta la última actividad
            var finEstimado = inicio.AddMinutes(5);
            var updateQuery = @"
                UPDATE OfflineSesionesQueue
                SET FechaHoraFin = @FechaHoraFin,
                    DuracionMinutos = 5,
                    TipoCierre = 7, -- ApagadoForzado
                    EstadoSync = 'Pendiente'
                WHERE Id = @Id;";

            using var updateCmd = new SqliteCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@Id", id);
            updateCmd.Parameters.AddWithValue("@FechaHoraFin", finEstimado.ToString("o"));
            await updateCmd.ExecuteNonQueryAsync();
        }
    }
}
