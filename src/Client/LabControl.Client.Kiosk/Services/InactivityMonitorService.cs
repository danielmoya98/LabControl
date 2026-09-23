using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace LabControl.Client.Kiosk.Services;

public class InactivityMonitorService
{
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    private DispatcherTimer? _timer;
    private int _minutosMaximos = 15;
    private bool _esApagar = true;
    private Action<bool>? _onTimeoutCallback;
    private InactivityWarningDialog? _warningDialog;

    public void Start(int minutosMaximos, bool esApagar, Action<bool> onTimeoutCallback)
    {
        Stop();

        _minutosMaximos = minutosMaximos > 0 ? minutosMaximos : 15;
        _esApagar = esApagar;
        _onTimeoutCallback = onTimeoutCallback;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;

        if (_warningDialog != null)
        {
            try { _warningDialog.Close(); } catch { }
            _warningDialog = null;
        }
    }

    public static double GetIdleSeconds()
    {
        var lii = new LASTINPUTINFO();
        lii.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));

        if (GetLastInputInfo(ref lii))
        {
            uint idleTicks = (uint)Environment.TickCount - lii.dwTime;
            return idleTicks / 1000.0;
        }
        return 0;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        double idleSeconds = GetIdleSeconds();
        double limiteSegundos = _minutosMaximos * 60;
        double umbralAdvertencia = limiteSegundos - 60; // 60 segundos antes de actuar

        if (umbralAdvertencia < 30) umbralAdvertencia = 30; // Mínimo 30 segundos de aviso

        if (idleSeconds >= limiteSegundos)
        {
            Stop();
            _onTimeoutCallback?.Invoke(_esApagar);
        }
        else if (idleSeconds >= umbralAdvertencia)
        {
            int segundosRestantes = (int)(limiteSegundos - idleSeconds);
            if (segundosRestantes < 1) segundosRestantes = 1;

            if (_warningDialog == null)
            {
                _warningDialog = new InactivityWarningDialog(_minutosMaximos, _esApagar, () =>
                {
                    _warningDialog = null;
                });
                _warningDialog.Show();
            }

            _warningDialog.ActualizarSegundos(segundosRestantes, _esApagar);
        }
        else
        {
            if (_warningDialog != null)
            {
                try { _warningDialog.Close(); } catch { }
                _warningDialog = null;
            }
        }
    }
}
