using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LabControl.Client.Kiosk.Services;

namespace LabControl.Client.Kiosk;

public partial class SessionWidgetWindow : Window
{
    private readonly Action<int> _onCerrarCallback; // int: 1 = Manual, 2 = FinPeriodo, 3 = AdminRemoto, 4 = Inactividad
    private DispatcherTimer? _countdownTimer;
    private TimeSpan _tiempoRestante;

    public SessionWidgetWindow(string emailEstudiante, string aulaNombre, int minutosLimite, Action<int> onCerrarCallback)
    {
        InitializeComponent();

        _onCerrarCallback = onCerrarCallback;

        TxtEmailEstudiante.Text = emailEstudiante;
        TxtDetalleAula.Text = $"{aulaNombre} | Terminal Activa";

        _tiempoRestante = TimeSpan.FromMinutes(minutosLimite > 0 ? minutosLimite : 90);
        ActualizarTextoTiempo();
        IniciarTimer();

        // Posicionar en la esquina superior derecha de la pantalla principal
        Left = SystemParameters.WorkArea.Right - Width - 25;
        Top = 25;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Activar desenfoque acrílico esmerilado detrás de la ventana con tinte oscuro y acento sutil
        WindowBlurHelper.EnableBlur(this, alpha: 170, r: 16, g: 22, b: 32);
    }

    private void IniciarTimer()
    {
        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += (s, e) =>
        {
            if (_tiempoRestante.TotalSeconds > 0)
            {
                _tiempoRestante = _tiempoRestante.Subtract(TimeSpan.FromSeconds(1));
                ActualizarTextoTiempo();
            }
            else
            {
                _countdownTimer.Stop();
                CerrarYNotificar(2); // 2 = FinPeriodo
            }
        };
        _countdownTimer.Start();
    }

    private void ActualizarTextoTiempo()
    {
        TxtTiempoRestante.Text = _tiempoRestante.Hours > 0
            ? _tiempoRestante.ToString(@"hh\:mm\:ss")
            : _tiempoRestante.ToString(@"mm\:ss");
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
            CerrarYNotificar(1); // 1 = Manual
        }
    }

    private void OnFinalizarYApagarClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "¿Desea finalizar su uso y apagar el equipo físico ahora?\n\nAl confirmar, su sesión se cerrará y la computadora se apagará para optimizar el consumo de energía del campus.",
            "Finalizar Clase y Apagar", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            CerrarYNotificar(1); // 1 = Manual

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/s /t 2 /f",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch
            {
                // Silencioso
            }
        }
    }

    public void CerrarPorComandoRemoto()
    {
        Dispatcher.Invoke(() =>
        {
            CerrarYNotificar(3); // 3 = AdminRemoto
        });
    }

    public void CerrarPorInactividad(bool apagar)
    {
        Dispatcher.Invoke(() =>
        {
            CerrarYNotificar(4); // 4 = Inactividad

            if (apagar)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/s /t 2 /f",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                catch
                {
                    // Silencioso
                }
            }
        });
    }

    private void CerrarYNotificar(int motivoCierre)
    {
        _countdownTimer?.Stop();
        this.Close();
        _onCerrarCallback?.Invoke(motivoCierre);
    }
}
