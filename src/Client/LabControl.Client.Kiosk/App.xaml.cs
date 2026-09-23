using System.Windows;
using LabControl.Client.Kiosk.Services;

namespace LabControl.Client.Kiosk;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Recuperar y cerrar sesiones truncadas por apagados forzados previos
        _ = SqliteOfflineService.RecuperarSesionesHuerfanasAsync();

        // Si ya existe kiosk-config.json se abre la ventana principal de bloqueo.
        // Si no existe, se abre la ventana de instalación para leer de la BD las Aulas disponibles.
        if (LocalStorageService.HasConfiguration())
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
