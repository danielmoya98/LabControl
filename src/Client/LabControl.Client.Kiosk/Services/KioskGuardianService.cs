using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Servicio Centinela (Watchdog Anti-Sabotaje) de doble proceso recíproco:
/// Protege al cliente Kiosk contra intentos de matar el proceso desde el Administrador de Tareas,
/// scripts de consola (taskkill), Process Hacker o intentos de sabotaje.
/// Si el proceso principal Kiosk es terminado abruptamente, el Guardian lo relanza en menos de 300 ms.
/// Si el Guardian es terminado, el Kiosk lo relanza en menos de 1000 ms.
/// </summary>
public static class KioskGuardianService
{
    private const string GracefulEventName = @"Global\LabControl_Kiosk_GracefulExit";
    private static EventWaitHandle? _gracefulEvent;
    private static Process? _guardianProcess;
    private static Timer? _guardianHealthCheckTimer;
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
            _guardianHealthCheckTimer?.Dispose();
            _guardianHealthCheckTimer = null;
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
    /// Bucle permanente en segundo plano del proceso Centinela (ejecutado con --guardian targetPid).
    /// Vigila continuamente el proceso Kiosk y termina cualquier herramienta de sabotaje.
    /// </summary>
    public static void RunGuardianLoop(int targetPid)
    {
        int currentTargetPid = targetPid;

        while (!IsGracefulShutdown())
        {
            try
            {
                // 1. Eliminar herramientas de sabotaje (Task Manager, Process Hacker, etc.)
                TaskManagerHelper.KillProhibitedProcesses();

                // 2. Verificar si el proceso Kiosk sigue vivo
                bool isAlive = false;
                try
                {
                    var targetProcess = Process.GetProcessById(currentTargetPid);
                    if (!targetProcess.HasExited)
                    {
                        isAlive = true;
                    }
                }
                catch
                {
                    isAlive = false;
                }

                if (!isAlive)
                {
                    // Si el cierre fue formal por técnico, salir limpiamente
                    if (IsGracefulShutdown())
                    {
                        break;
                    }

                    // Intento de sabotaje detectado: el proceso Kiosk fue terminado de forma violenta.
                    // Relanzar inmediatamente LabControl.Client.Kiosk.exe para restaurar el bloqueo.
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    var kioskExe = Path.Combine(appDir, "LabControl.Client.Kiosk.exe");

                    if (!File.Exists(kioskExe))
                    {
                        kioskExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    }

                    if (!string.IsNullOrEmpty(kioskExe) && File.Exists(kioskExe))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = kioskExe,
                            Arguments = "--relaunch-after-kill",
                            UseShellExecute = true
                        };

                        var newKiosk = Process.Start(psi);
                        if (newKiosk != null)
                        {
                            currentTargetPid = newKiosk.Id;
                        }
                    }
                }

                Thread.Sleep(300);
            }
            catch
            {
                Thread.Sleep(1000);
            }
        }
    }

    /// <summary>
    /// Inicia el proceso centinela Guardian en segundo plano para blindar la ejecución del Kiosk.
    /// Se lanza de forma desacoplada (UseShellExecute = true) para que la muerte del Kiosk no mate al Guardian.
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
                    UseShellExecute = true
                };

                _guardianProcess = Process.Start(psi);

                // Activar verificación de salud periódica cada 1 segundo (Blindaje Recíproco)
                _guardianHealthCheckTimer?.Dispose();
                _guardianHealthCheckTimer = new Timer(_ =>
                {
                    if (IsGracefulShutdown()) return;

                    bool needRestart = false;
                    lock (_lock)
                    {
                        if (_guardianProcess == null || _guardianProcess.HasExited)
                        {
                            needRestart = true;
                        }
                    }

                    if (needRestart && !IsGracefulShutdown())
                    {
                        StartGuardian();
                    }
                }, null, 1000, 1000);
            }
            catch
            {
                // Silencioso
            }
        }
    }
}
