using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Servicio Centinela (Watchdog Anti-Sabotaje) de doble proceso:
/// Protege al cliente Kiosk contra intentos de matar el proceso desde el Administrador de Tareas,
/// scripts de consola (taskkill), Process Hacker o malware de estudiantes.
/// Si el proceso principal es terminado abruptamente, el Guardian lo relanza en menos de 500 ms
/// y re-bloquea la pantalla de inmediato.
/// </summary>
public static class KioskGuardianService
{
    private const string GracefulEventName = @"Global\LabControl_Kiosk_GracefulExit";
    private static EventWaitHandle? _gracefulEvent;
    private static Process? _guardianProcess;
    private static readonly object _lock = new();

    static KioskGuardianService()
    {
        try
        {
            _gracefulEvent = new EventWaitHandle(false, EventResetMode.ManualReset, GracefulEventName);
        }
        catch
        {
            try
            {
                _gracefulEvent = new EventWaitHandle(false, EventResetMode.ManualReset, @"Local\LabControl_Kiosk_GracefulExit");
            }
            catch { }
        }
    }

    /// <summary>
    /// Señaliza que el cierre es legítimo y autorizado por un técnico con contraseña maestra.
    /// Evita que el Guardian relance la aplicación cuando el administrador decide cerrarla.
    /// </summary>
    public static void SignalGracefulShutdown()
    {
        try
        {
            _gracefulEvent?.Set();
        }
        catch { }

        try
        {
            if (_guardianProcess != null && !_guardianProcess.HasExited)
            {
                _guardianProcess.Kill();
            }
        }
        catch { }
    }

    public static bool IsGracefulShutdown()
    {
        try
        {
            return _gracefulEvent != null && _gracefulEvent.WaitOne(0);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Bucle en segundo plano del proceso Centinela (ejecutado silenciosamente con --guardian targetPid).
    /// </summary>
    public static void RunGuardianLoop(int targetPid)
    {
        try
        {
            Process? targetProcess = null;
            try
            {
                targetProcess = Process.GetProcessById(targetPid);
            }
            catch
            {
                // El proceso ya no existe
            }

            if (targetProcess != null)
            {
                targetProcess.WaitForExit();
            }

            // Si el cierre fue autorizado formalmente por el técnico, terminar silenciosamente
            if (IsGracefulShutdown())
            {
                return;
            }

            // Intento de sabotaje detectado: el proceso Kiosk fue terminado de forma violenta (taskkill, fin de tarea)
            // Relanzar inmediatamente LabControl.Client.Kiosk.exe para restaurar el bloqueo
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var kioskExe = Path.Combine(appDir, "LabControl.Client.Kiosk.exe");

            if (!File.Exists(kioskExe))
            {
                kioskExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            }

            if (!string.IsNullOrEmpty(kioskExe) && File.Exists(kioskExe))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = kioskExe,
                    Arguments = "--relaunch-after-kill",
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Silencioso
        }
    }

    /// <summary>
    /// Inicia el proceso centinela Guardian en segundo plano para blindar la ejecución del Kiosk.
    /// </summary>
    public static void StartGuardian()
    {
        lock (_lock)
        {
            try
            {
                if (_guardianProcess != null && !_guardianProcess.HasExited)
                {
                    return;
                }

                _gracefulEvent?.Reset();

                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var guardianExe = Path.Combine(appDir, "LabControl.Guardian.exe");
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;

                var exeToRun = File.Exists(guardianExe) ? guardianExe : currentExe;
                if (string.IsNullOrEmpty(exeToRun) || !File.Exists(exeToRun)) return;

                int myPid = Process.GetCurrentProcess().Id;

                var psi = new ProcessStartInfo
                {
                    FileName = exeToRun,
                    Arguments = $"--guardian {myPid}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                };

                _guardianProcess = Process.Start(psi);

                if (_guardianProcess != null)
                {
                    _guardianProcess.EnableRaisingEvents = true;
                    _guardianProcess.Exited += (s, e) =>
                    {
                        // Si un estudiante mata el Guardian, el Kiosk lo vuelve a levantar de inmediato (Blindaje Mutuo)
                        if (!IsGracefulShutdown())
                        {
                            Thread.Sleep(300);
                            StartGuardian();
                        }
                    };
                }
            }
            catch
            {
                // Silencioso
            }
        }
    }
}
