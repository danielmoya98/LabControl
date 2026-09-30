using System.Windows;

namespace LabControl.Client.Kiosk;

public partial class AlertaMensajeDialog : Window
{
    public AlertaMensajeDialog(string mensaje)
    {
        InitializeComponent();
        TxtMensaje.Text = mensaje;
        TxtTimestamp.Text = $"Recibido a las {DateTime.Now:HH:mm:ss} — Control Central";
        Loaded += (s, e) => Services.WindowBlurHelper.EnableBlur(this, alpha: 200, r: 15, g: 23, b: 42);
    }

    private void OnAceptarClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
