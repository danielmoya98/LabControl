using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LabControl.Client.Kiosk.Services;
using LabControl.Domain.Enums;

namespace LabControl.Client.Kiosk;

public partial class SessionWidgetWindow : Window
{
    private readonly Action<int> _onCerrarCallback;
    private readonly Action _onApagarCallback;
    private bool _isAuthorizedClose;

    public SessionWidgetWindow(
        string emailEstudiante,
        string aulaNombre,
        string hostname,
        Action<int> onCerrarCallback,
        Action onApagarCallback)
    {
        InitializeComponent();

        _onCerrarCallback = onCerrarCallback;
        _onApagarCallback = onApagarCallback;

        TxtEmailEstudiante.Text = emailEstudiante;
        TxtDetalleAula.Text = $"{aulaNombre} • {hostname}";

        // Posicionar en la esquina superior derecha de la pantalla principal
        Left = SystemParameters.WorkArea.Right - Width - 25;
        Top = 25;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Activar desenfoque acrílico esmerilado detrás de la ventana con tinte oscuro y acento sutil
        WindowBlurHelper.EnableBlur(this, alpha: 170, r: 16, g: 22, b: 32);
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Bloquear atajo Alt+F4 para evitar el cierre intencional del widget
        if (e.Key == Key.System && e.SystemKey == Key.F4)
        {
            e.Handled = true;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Blindaje contra cierre no autorizado (WM_CLOSE desde Administrador de Tareas o atajos de teclado)
        if (!_isAuthorizedClose && !KioskGuardianService.IsGracefulShutdown())
        {
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }

    public void CloseAuthorized()
    {
        _isAuthorizedClose = true;
        Close();
    }

    private void OnBorderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OnCerrarSesionClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "¿Desea cerrar su sesión para permitir que otro estudiante ingrese?\n\nLa terminal permanecerá encendida y bloqueada.",
            "Confirmar Cambio de Usuario", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            CloseAuthorized();
            _onCerrarCallback?.Invoke((int)TipoCierreSesion.Manual);
        }
    }

    private void OnFinalizarYApagarClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "¿Desea finalizar su uso y apagar el equipo físico ahora?\n\nAl confirmar, su sesión se cerrará y la computadora se apagará para optimizar el consumo de energía del campus.",
            "Finalizar y Apagar", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            CloseAuthorized();
            _onApagarCallback?.Invoke();
        }
    }

    public void ActualizarDetalle(string aulaNombre, string hostname)
    {
        TxtDetalleAula.Text = $"{aulaNombre} • {hostname}";
    }
}
