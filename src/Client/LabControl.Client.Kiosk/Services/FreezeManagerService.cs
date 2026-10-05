using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Gestiona la detección y control del estado de congelamiento de disco (Deep Freeze / Windows UWF).
/// Permite descongelar (Thaw) y congelar (Freeze) la terminal de forma remota para labores de mantenimiento.
/// </summary>
public static class FreezeManagerService
{
    private static readonly string[] PosiblesRutasDfc = new[]
    {
        @"C:\Windows\System32\DFC.exe",
        @"C:\Windows\DFC.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Faronics\Deep Freeze\Install Sys\DFC.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Faronics\Deep Freeze\Install Sys\DFC.exe"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DFC.exe")
    };

    public static string? LocalizarDfcExe()
    {
        foreach (var ruta in PosiblesRutasDfc)
        {
            if (File.Exists(ruta)) return ruta;
        }

        // Buscar en PATH del sistema
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var p in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var testPath = Path.Combine(p.Trim(), "DFC.exe");
            if (File.Exists(testPath)) return testPath;
        }

        return null;
    }

    public static bool TieneWindowsUwf()
    {
        var uwfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "uwfmgr.exe");
        return File.Exists(uwfPath);
    }

    /// <summary>
    /// Consulta el estado actual de congelado del equipo.
    /// Retorna: "Congelado", "Descongelado", o "No Detectado"
    /// </summary>
    public static async Task<string> ObtenerEstadoFreezeAsync()
    {
        try
        {
            var dfcPath = LocalizarDfcExe();
            if (dfcPath != null)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = dfcPath,
                    Arguments = "get /STATUS",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    var salida = await proc.StandardOutput.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (salida.Contains("THAWED", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Descongelado";
                    }
                    if (salida.Contains("FROZEN", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Congelado";
                    }
                }
            }

            if (TieneWindowsUwf())
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "uwfmgr.exe",
                    Arguments = "filter get-status",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    var salida = await proc.StandardOutput.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (salida.Contains("ON", StringComparison.OrdinalIgnoreCase) || salida.Contains("ENABLED", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Congelado";
                    }
                    if (salida.Contains("OFF", StringComparison.OrdinalIgnoreCase) || salida.Contains("DISABLED", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Descongelado";
                    }
                }
            }

            return "No Detectado";
        }
        catch
        {
            return "No Detectado";
        }
    }

    /// <summary>
    /// Ejecuta la orden de Descongelar (Thaw) y reinicia el equipo para aplicar cambios si se solicita.
    /// </summary>
    public static async Task<(bool Exito, string Mensaje)> DescongelarAsync(string? clave, bool reiniciar = true)
    {
        try
        {
            var dfcPath = LocalizarDfcExe();
            if (dfcPath != null)
            {
                var pass = string.IsNullOrWhiteSpace(clave) ? "" : clave.Trim();
                var args = string.IsNullOrEmpty(pass) ? "/BOOTTHAWED" : $"{pass} /BOOTTHAWED";

                var psi = new ProcessStartInfo
                {
                    FileName = dfcPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    var salida = await proc.StandardOutput.ReadToEndAsync();
                    var error = await proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (reiniciar)
                    {
                        ProgramarReinicio("Reinicio automático para descongelar la terminal.");
                    }

                    return (true, $"Deep Freeze configurado en modo Descongelado. {salida} {error}".Trim());
                }
            }

            if (TieneWindowsUwf())
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "uwfmgr.exe",
                    Arguments = "filter disable",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    if (reiniciar)
                    {
                        ProgramarReinicio("Reinicio para aplicar filtro UWF descongelado.");
                    }
                    return (true, "Filtro Windows UWF deshabilitado. Se requiere reinicio.");
                }
            }

            return (false, "No se encontró Deep Freeze (DFC.exe) ni Windows UWF instalado en este equipo.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al ejecutar comando de descongelado: {ex.Message}");
        }
    }

    /// <summary>
    /// Ejecuta la orden de Congelar (Freeze) y reinicia el equipo si se solicita.
    /// </summary>
    public static async Task<(bool Exito, string Mensaje)> CongelarAsync(string? clave, bool reiniciar = true)
    {
        try
        {
            var dfcPath = LocalizarDfcExe();
            if (dfcPath != null)
            {
                var pass = string.IsNullOrWhiteSpace(clave) ? "" : clave.Trim();
                var args = string.IsNullOrEmpty(pass) ? "/BOOTFROZEN" : $"{pass} /BOOTFROZEN";

                var psi = new ProcessStartInfo
                {
                    FileName = dfcPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    var salida = await proc.StandardOutput.ReadToEndAsync();
                    var error = await proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (reiniciar)
                    {
                        ProgramarReinicio("Reinicio automático para congelar y proteger la terminal.");
                    }

                    return (true, $"Deep Freeze configurado en modo Congelado. {salida} {error}".Trim());
                }
            }

            if (TieneWindowsUwf())
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "uwfmgr.exe",
                    Arguments = "filter enable",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    if (reiniciar)
                    {
                        ProgramarReinicio("Reinicio para activar protección de disco UWF.");
                    }
                    return (true, "Filtro Windows UWF habilitado. Se requiere reinicio.");
                }
            }

            return (false, "No se encontró software de congelamiento compatible.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al ejecutar comando de congelado: {ex.Message}");
        }
    }

    private static void ProgramarReinicio(string mensaje)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = $"/r /t 5 /c \"{mensaje}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        catch { }
    }
}
