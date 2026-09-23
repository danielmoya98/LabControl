using System;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace LabControl.Client.Kiosk;

public partial class NotificationToastWindow : Window
{
    private readonly DispatcherTimer _autoCloseTimer;

    public NotificationToastWindow(string mensaje)
    {
        InitializeComponent();

        TxtMensaje.Text = mensaje;

        // Posicionar en la esquina inferior derecha de la pantalla principal
        Left = SystemParameters.WorkArea.Right - Width - 20;
        Top = SystemParameters.WorkArea.Bottom - Height - 20;

        // Sonido suave de notificación de Windows
        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch { }

        // Cerrar automáticamente tras 12 segundos
        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(12)
        };
        _autoCloseTimer.Tick += (s, e) =>
        {
            _autoCloseTimer.Stop();
            Close();
        };
        _autoCloseTimer.Start();
    }

    private void OnBorderMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Al hacer clic sobre el banner, se cierra
        _autoCloseTimer.Stop();
        Close();
    }

    private void OnCerrarClick(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer.Stop();
        Close();
    }
}
