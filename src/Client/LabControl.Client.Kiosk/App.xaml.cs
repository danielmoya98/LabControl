using System.Windows;
using LabControl.Client.Kiosk.Services;

namespace LabControl.Client.Kiosk;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Si ya existe kiosk-config.json se abre la ventana principal de bloqueo.
        // Si no existe, se abre la ventana de instalación para leer de la BD las Aulas disponibles.
        if (LocalStorageService.HasConfiguration())
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        else
        {
            var setupWindow = new SetupWindow();
            setupWindow.Show();
        }
    }
}
