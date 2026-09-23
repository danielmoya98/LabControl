using System;
using System.Windows;

namespace LabControl.Client.Kiosk;

public partial class InactivityWarningDialog : Window
{
    private readonly Action _onActividadConfirmada;

    public InactivityWarningDialog(int minutosInactivos, bool esApagar, Action onActividadConfirmada)
    {
        InitializeComponent();
        _onActividadConfirmada = onActividadConfirmada;

        string accionStr = esApagar ? "se apagará automáticamente para ahorrar energía" : "cerrará la sesión y se bloqueará";
        TxtMensaje.Text = $"No se ha detectado actividad física en los últimos {minutosInactivos} minutos. Por políticas del laboratorio, el equipo {accionStr}.";
    }

    public void ActualizarSegundos(int segundosRestantes, bool esApagar)
    {
        string accionStr = esApagar ? "apagará" : "bloqueará";
        TxtSegundosRestantes.Text = $"La terminal se {accionStr} en {segundosRestantes} segundos...";
    }

    private void OnSigoTrabajandoClick(object sender, RoutedEventArgs e)
    {
        _onActividadConfirmada?.Invoke();
        this.Close();
    }
}
