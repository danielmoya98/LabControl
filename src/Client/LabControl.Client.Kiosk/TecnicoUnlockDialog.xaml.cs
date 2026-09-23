using System.Windows;
using System.Windows.Input;
using LabControl.Client.Kiosk.Services;

namespace LabControl.Client.Kiosk;

public partial class TecnicoUnlockDialog : Window
{
    private readonly string _claveMaestra;
    private readonly Window _mainWindow;

    public bool DesbloqueoExitoso { get; private set; }

    public TecnicoUnlockDialog(Window mainWindow, string? claveConfigurada = null)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        // Clave por defecto institucional si no está personalizada
        _claveMaestra = !string.IsNullOrWhiteSpace(claveConfigurada) ? claveConfigurada : "AdminLab@2026";
        TxtPassword.Focus();
    }

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ValidarClave();
        }
    }

    private void OnValidarClaveClick(object sender, RoutedEventArgs e)
    {
        ValidarClave();
    }

    private void ValidarClave()
    {
        var pass = TxtPassword.Password;
        if (pass == _claveMaestra)
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            TxtPassword.IsEnabled = false;
            BtnValidar.Visibility = Visibility.Collapsed;
            PanelOpciones.Visibility = Visibility.Visible;
            DesbloqueoExitoso = true;
        }
        else
        {
            TxtError.Text = "⚠️ Contraseña incorrecta. Acceso denegado.";
            ErrorBorder.Visibility = Visibility.Visible;
            TxtPassword.SelectAll();
            TxtPassword.Focus();
        }
    }

    private void OnDesbloquearMinimizarClick(object sender, RoutedEventArgs e)
    {
        // Desactivar hooks y habilitar task manager
        WindowsHookManager.UninstallHook();
        TaskManagerHelper.EnableTaskManager();

        _mainWindow.WindowState = WindowState.Minimized;
        _mainWindow.Topmost = false;

        MessageBox.Show("Terminal desbloqueada en modo mantenimiento.\nPara reactivar el Kiosk, restaure la ventana de la barra de tareas.",
            "Modo Mantenimiento Activo", MessageBoxButton.OK, MessageBoxImage.Information);

        Close();
    }

    private void OnAbrirConfiguracionClick(object sender, RoutedEventArgs e)
    {
        KioskGuardianService.SignalGracefulShutdown();
        WindowsHookManager.UninstallHook();
        TaskManagerHelper.EnableTaskManager();

        var setupWindow = new SetupWindow();
        setupWindow.Show();
        _mainWindow.Close();
        Close();
    }

    private void OnCerrarAppClick(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show("¿Está seguro de cerrar por completo el cliente Kiosk?",
            "Cerrar Kiosk", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (res == MessageBoxResult.Yes)
        {
            KioskGuardianService.SignalGracefulShutdown();
            WindowsHookManager.UninstallHook();
            TaskManagerHelper.EnableTaskManager();
            Application.Current.Shutdown();
        }
    }

    private void OnCancelarClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
