using System.Windows;
using System.Windows.Input;
using LabControl.Client.Kiosk.Services;

namespace LabControl.Client.Kiosk;

public partial class SecondaryMonitorBlockerWindow : Window
{
    private readonly MainWindow _mainWindow;

    public SecondaryMonitorBlockerWindow(MonitorInfo monitor, MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = monitor.Left;
        Top = monitor.Top;
        Width = monitor.Width;
        Height = monitor.Height;

        Loaded += (s, e) =>
        {
            WindowState = WindowState.Maximized;
            Topmost = true;
        };
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _mainWindow.Activate();
        _mainWindow.Focus();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Enviar foco a la ventana principal para que cualquier tecla vaya al formulario de login
        e.Handled = true;
        _mainWindow.Activate();
        _mainWindow.Focus();
    }
}
