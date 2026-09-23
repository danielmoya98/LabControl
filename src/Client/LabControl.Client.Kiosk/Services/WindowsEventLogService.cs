using System;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks;

namespace LabControl.Client.Kiosk.Services;

public class ApagadoForzadoEventoInfo
{
    public int EventId { get; set; }
    public DateTime FechaHoraUtc { get; set; }
    public string Detalle { get; set; } = "";
}

/// <summary>
/// Servicio de auditoría para inspeccionar el Visor de Eventos de Windows (Event Log)
/// y certificar eventos de apagado sucio, cortes de energía o pulsaciones prolongadas
/// de botón de encendido (Kernel-Power Event ID 41 / EventLog Event ID 6008).
/// </summary>
public static class WindowsEventLogService
{
    /// <summary>
    /// Examina el registro del sistema en busca de eventos de corte inesperado de energía
    /// ocurridos con posterioridad al último timestamp reportado.
    /// </summary>
    public static ApagadoForzadoEventoInfo? ObtenerUltimoApagadoInesperado(DateTime? desdeFechaUtc = null)
    {
        try
        {
            // Event ID 41: Kernel-Power (Corte súbito de alimentación o cuelgue)
            // Event ID 6008: EventLog (El apagado anterior fue inesperado)
            string queryXPath = "*[System[(EventID=41 or EventID=6008)]]";
            var query = new EventLogQuery("System", PathType.LogName, queryXPath)
            {
                ReverseDirection = true // Los más recientes primero
            };

            using var reader = new EventLogReader(query);
            EventRecord? record;

            DateTime umbralMinimo = desdeFechaUtc ?? DateTime.UtcNow.Subtract(TimeSpan.FromDays(2));

            while ((record = reader.ReadEvent()) != null)
            {
                using (record)
                {
                    if (record.TimeCreated.HasValue)
                    {
                        var fechaEventoUtc = record.TimeCreated.Value.ToUniversalTime();

                        // Si el evento ocurrió después de nuestro último umbral registrado
                        if (fechaEventoUtc > umbralMinimo)
                        {
                            string desc = record.Id == 41
                                ? "Corte súbito de energía física / botón de encendido forzado (Kernel-Power Event 41)"
                                : "Apagado no limpio o corte de energía detectado (EventLog 6008)";

                            return new ApagadoForzadoEventoInfo
                            {
                                EventId = record.Id,
                                FechaHoraUtc = fechaEventoUtc,
                                Detalle = desc
                            };
                        }
                        else
                        {
                            // Al estar ordenado de más reciente a más antiguo, paramos al cruzar el umbral
                            break;
                        }
                    }
                }
            }
        }
        catch
        {
            // Silencioso en caso de permisos restringidos en cuentas de usuario estándar
        }

        return null;
    }

    /// <summary>
    /// Certifica y reporta a la API central si la computadora sufrió un apagado de fuerza bruta
    /// antes del arranque actual.
    /// </summary>
    public static async Task VerificarYReportarApagadoForzadoAsync(KioskApiService? apiService, ConfigModel? config)
    {
        if (apiService == null || config == null || string.IsNullOrWhiteSpace(config.Hostname))
            return;

        try
        {
            var ultimoEvento = ObtenerUltimoApagadoInesperado(config.UltimoApagadoReportadoUtc);
            if (ultimoEvento != null)
            {
                bool reportado = await apiService.ReportarApagadoForzadoAsync(
                    config.Hostname,
                    ultimoEvento.FechaHoraUtc,
                    ultimoEvento.EventId,
                    ultimoEvento.Detalle,
                    config.UltimoEstudianteSesion
                );

                if (reportado)
                {
                    config.UltimoApagadoReportadoUtc = ultimoEvento.FechaHoraUtc;
                    // Ya reportado el incidente, limpiar la memoria del último usuario activo
                    config.UltimoEstudianteSesion = null;
                    LocalStorageService.SaveConfig(config);
                }
            }
        }
        catch
        {
            // Silencioso
        }
    }
}
