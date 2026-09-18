using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace LabControl.Client.Kiosk;

public partial class SessionWidgetWindow : Window
{
    private readonly DispatcherTimer _timer;
    private TimeSpan _tiempoRestante;
    private readonly Action<int> _onCerrarCallback; // int: 1 = Manual, 2 = FinPeriodo

    public SessionWidgetWindow(string emailEstudiante, string aulaNombre, int minutosLimite, Action<int> onCerrarCallback)
    {
        InitializeComponent();

        _onCerrarCallback = onCerrarCallback;
        _tiempoRestante = TimeSpan.FromMinutes(minutosLimite);

        TxtEmailEstudiante.Text = emailEstudiante;
        TxtDetalleAula.Text = $"{aulaNombre} | Activo";
        ActualizarTextoContador();

        // Posicionar en la esquina superior derecha
        Left = SystemParameters.WorkArea.Right - Width - 25;
        Top = 25;

        // Configurar timer a 1 segundo
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_tiempoRestante.TotalSeconds > 0)
        {
            _tiempoRestante = _tiempoRestante.Subtract(TimeSpan.FromSeconds(1));
            ActualizarTextoContador();

            // Advertencias visuales según tiempo restante
            if (_tiempoRestante.TotalMinutes <= 1)
            {
                TxtContador.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (_tiempoRestante.TotalMinutes <= 5)
            {
                TxtContador.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else if (_tiempoRestante.TotalMinutes <= 10)
            {
                TxtContador.Foreground = System.Windows.Media.Brushes.Yellow;
            }
        }
        else
        {
            _timer.Stop();
            MessageBox.Show("⏰ El tiempo de uso asignado ha concluido. La terminal se bloqueará automáticamente.",
                "Fin de Sesión", MessageBoxButton.OK, MessageBoxImage.Warning);
            CerrarYNotificar(2); // 2 = FinPeriodo
        }
    }

    private void ActualizarTextoContador()
    {
        TxtContador.Text = _tiempoRestante.ToString(@"hh\:mm\:ss");
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
        var result = MessageBox.Show("¿Está seguro que desea cerrar su sesión y bloquear la terminal?",
            "Confirmar Cierre de Sesión", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _timer.Stop();
            CerrarYNotificar(1); // 1 = Manual
        }
    }

    public void CerrarPorComandoRemoto()
    {
        _timer.Stop();
        Dispatcher.Invoke(() =>
        {
            CerrarYNotificar(5); // 5 = AdminRemoto
        });
    }

    private void CerrarYNotificar(int tipoCierre)
    {
        _onCerrarCallback.Invoke(tipoCierre);
        Close();
    }
}
