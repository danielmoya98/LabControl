using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Gestiona la descarga, verificación de integridad y aplicación en caliente
/// de actualizaciones remotas (OTA) para el cliente Kiosk.
/// </summary>
public static class UpdateDownloaderService
{
    public static async Task<(bool Exito, string Mensaje)> AplicarActualizacionRemotaAsync(
        string apiBaseUrl,
        string urlDescarga,
        string nuevaVersion,
        string? sha256Esperado)
    {
        try
        {
            var updateDir = Path.Combine(Path.GetTempPath(), "LabControlUpdates");
            if (!Directory.Exists(updateDir))
            {
                Directory.CreateDirectory(updateDir);
            }

            var downloadUrlCompleta = urlDescarga.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? urlDescarga
                : $"{apiBaseUrl.TrimEnd('/')}/{urlDescarga.TrimStart('/')}";

            var tempFileName = $"LabControl_Update_{DateTime.UtcNow:yyyyMMddHHmmss}.exe";
            var tempFilePath = Path.Combine(updateDir, tempFileName);

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            using var response = await httpClient.GetAsync(downloadUrlCompleta, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return (false, $"Error al descargar el paquete de actualización HTTP {(int)response.StatusCode}.");
            }

            await using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs);
            }

            // Validar integridad SHA-256 si se proporcionó
            if (!string.IsNullOrWhiteSpace(sha256Esperado))
            {
                using var sha = SHA256.Create();
                await using var stream = File.OpenRead(tempFilePath);
                var hashBytes = await sha.ComputeHashAsync(stream);
                var hashCalculado = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                if (!hashCalculado.Equals(sha256Esperado.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(tempFilePath); } catch { }
                    return (false, "El archivo descargado no coincide con el hash de seguridad SHA-256 esperado.");
                }
            }

            // Preparar script de reemplazo en caliente
            var currentExePath = Process.GetCurrentProcess().MainModule?.FileName 
                                 ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LabControl.Client.Kiosk.exe");
            var currentPid = Process.GetCurrentProcess().Id;
            var scriptPath = Path.Combine(updateDir, $"apply_update_{currentPid}.cmd");

            // Si el archivo descargado es un instalador Setup de Inno Setup
            bool esInstaladorSetup = tempFilePath.Contains("Setup", StringComparison.OrdinalIgnoreCase)
                                     || new FileInfo(tempFilePath).Length > 15_000_000;

            string cmdContent;
            if (esInstaladorSetup)
            {
                cmdContent = $@"@echo off
timeout /t 2 /nobreak > nul
taskkill /F /PID {currentPid} > nul 2>&1
timeout /t 1 /nobreak > nul
start /wait """" ""{tempFilePath}"" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
timeout /t 2 /nobreak > nul
start """" ""{currentExePath}""
del ""{tempFilePath}"" > nul 2>&1
(goto) 2>nul & del ""%~f0""
exit
";
            }
            else
            {
                cmdContent = $@"@echo off
timeout /t 2 /nobreak > nul
taskkill /F /PID {currentPid} > nul 2>&1
timeout /t 1 /nobreak > nul
copy /Y ""{tempFilePath}"" ""{currentExePath}"" > nul
timeout /t 1 /nobreak > nul
start """" ""{currentExePath}""
del ""{tempFilePath}"" > nul 2>&1
(goto) 2>nul & del ""%~f0""
exit
";
            }

            await File.WriteAllTextAsync(scriptPath, cmdContent);

            // Señalizar cierre legítimo para que Guardian no interfiera
            KioskGuardianService.SignalGracefulShutdown();

            // Lanzar el script actualizador desacoplado del proceso actual
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(psi);

            // Terminar la instancia actual de forma ordenada
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });

            return (true, "Actualización iniciada con éxito. El cliente se reiniciará en breve.");
        }
        catch (Exception ex)
        {
            return (false, $"Fallo durante el proceso de actualización remota: {ex.Message}");
        }
    }
}
