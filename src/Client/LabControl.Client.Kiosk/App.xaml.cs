using System.Windows;
using LabControl.Client.Kiosk.Services;

namespace LabControl.Client.Kiosk;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            RegistrarErrorFatal(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
        };

        DispatcherUnhandledException += (s, e) =>
        {
            RegistrarErrorFatal(e.Exception, "DispatcherUnhandledException");
            e.Handled = true;
        };
    }

    private static void RegistrarErrorFatal(Exception? ex, string source)
    {
        if (ex == null) return;
        try
        {
            var logPath = System.IO.Path.Combine(LocalStorageService.GetDataDirectory(), "kiosk-crash.log");
            var errorText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            System.IO.File.AppendAllText(logPath, errorText);

            MessageBox.Show(
                $"Se produjo un error al ejecutar LabControl Kiosk:\n\n{ex.Message}\n\nOrigen: {source}\nConsulte el archivo:\n{logPath}",
                "LabControl Kiosk - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Silencioso
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // 0. Si se invoca en modo centinela Guardian, ejecutar el bucle protector sin UI
            var guardianIndex = Array.FindIndex(e.Args, a => string.Equals(a, "--guardian", StringComparison.OrdinalIgnoreCase));
            if (guardianIndex >= 0 && e.Args.Length > guardianIndex + 1 && int.TryParse(e.Args[guardianIndex + 1], out int targetPid))
            {
                KioskGuardianService.RunGuardianLoop(targetPid);
                Shutdown(0);
                return;
            }

            // Iniciar Guardian para blindar el Kiosk contra terminaciones forzadas de proceso
            KioskGuardianService.StartGuardian();

            // Recuperar y cerrar sesiones truncadas por apagados forzados previos
            _ = SqliteOfflineService.RecuperarSesionesHuerfanasAsync();

            bool forzarSetup = e.Args.Any(a => string.Equals(a, "--setup", StringComparison.OrdinalIgnoreCase) ||
                                               string.Equals(a, "/setup", StringComparison.OrdinalIgnoreCase));

            // Si ya está configurado con aula válida y no se forzó el asistente, se abre la ventana principal de bloqueo.
            // En caso contrario, se abre la ventana de instalación para leer de la BD las Aulas disponibles.
            if (LocalStorageService.HasConfiguration() && !forzarSetup)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Show();
            }
            else
            {
                var setupWindow = new SetupWindow();
                setupWindow.Show();
            }
        }
        catch (Exception ex)
        {
            RegistrarErrorFatal(ex, "OnStartup");
        }
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        try
        {
            _mainWindow?.FinalizarSesionPorApagadoSistema();
        }
        catch
        {
            // Silencioso
        }

        WindowsHookManager.UninstallHook();
        TaskManagerHelper.EnableTaskManager();
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        WindowsHookManager.UninstallHook();
        TaskManagerHelper.EnableTaskManager();
        base.OnExit(e);
    }
}
