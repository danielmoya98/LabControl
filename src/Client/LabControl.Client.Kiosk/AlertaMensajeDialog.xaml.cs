using System.Windows;

namespace LabControl.Client.Kiosk;

public partial class AlertaMensajeDialog : Window
{
    public AlertaMensajeDialog(string mensaje)
    {
        InitializeComponent();
        TxtMensaje.Text = mensaje;
        TxtTimestamp.Text = $"Recibido a las {DateTime.Now:HH:mm:ss} — Control Central";
    }

    private void OnAceptarClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
