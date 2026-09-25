namespace LabControl.Domain.Common;

/// <summary>
/// Proveedor centralizado de zona horaria institucional para Universidad del Valle (Bolivia, UTC-4).
/// Garantiza consistencia horaria exacta en Backend, Kiosk y WebAdmin independientemente
/// de la configuración de zona horaria del sistema operativo donde se ejecuten.
/// </summary>
public static class TimeZoneHelper
{
    public static readonly TimeZoneInfo BoliviaTimeZone = ObtenerZonaHorariaBolivia();

    private static TimeZoneInfo ObtenerZonaHorariaBolivia()
    {
        try
        {
            // Nombre de zona horaria en Windows
            return TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");
        }
        catch
        {
            try
            {
                // Nombre de zona horaria IANA estándar en Linux / macOS / Docker
                return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
            }
            catch
            {
                // Fallback seguro con offset constante UTC-4 (Bolivia no tiene horario de verano)
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Bolivia_Standard_Time",
                    TimeSpan.FromHours(-4),
                    "Hora de Bolivia (UTC-4)",
                    "Hora de Bolivia (UTC-4)"
                );
            }
        }
    }

    /// <summary>
    /// Convierte una fecha UTC a la hora local oficial de Bolivia (UTC-4).
    /// </summary>
    public static DateTime ToBoliviaTime(this DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc 
            ? utcDateTime 
            : (utcDateTime.Kind == DateTimeKind.Local ? utcDateTime.ToUniversalTime() : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));
        
        return TimeZoneInfo.ConvertTimeFromUtc(utc, BoliviaTimeZone);
    }

    /// <summary>
    /// Convierte una fecha UTC nullable a la hora local oficial de Bolivia (UTC-4).
    /// </summary>
    public static DateTime? ToBoliviaTime(this DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue) return null;
        return utcDateTime.Value.ToBoliviaTime();
    }

    /// <summary>
    /// Obtiene la fecha y hora actual en la zona horaria de Bolivia.
    /// </summary>
    public static DateTime NowBolivia => DateTime.UtcNow.ToBoliviaTime();
}
