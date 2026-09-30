using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LabControl.Client.Kiosk.Services;

internal enum AccentState
{
    ACCENT_DISABLED = 0,
    ACCENT_ENABLE_GRADIENT = 1,
    ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
    ACCENT_ENABLE_BLURBEHIND = 3,
    ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
    ACCENT_INVALID_STATE = 5
}

[StructLayout(LayoutKind.Sequential)]
internal struct AccentPolicy
{
    public AccentState AccentState;
    public int AccentFlags;
    public uint GradientColor;
    public int AnimationId;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WindowCompositionAttributeData
{
    public WindowCompositionAttribute Attribute;
    public IntPtr Data;
    public int SizeOfData;
}

internal enum WindowCompositionAttribute
{
    WCA_ACCENT_POLICY = 19
}

/// <summary>
/// Helper para aplicar desenfoque acrílico (Acrylic / Blur Behind / Glassmorphism)
/// nativo de Windows (DWM) en ventanas flotantes y modales de WPF.
/// Soporta Windows 11 (DWM System Backdrop) y Windows 10 (SetWindowCompositionAttribute).
/// </summary>
public static class WindowBlurHelper
{
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    // Valores para DWMWA_SYSTEMBACKDROP_TYPE (Windows 11 22H2 build 22621+)
    private const int DWMSBT_AUTO = 0;
    private const int DWMSBT_NONE = 1;
    private const int DWMSBT_MAINWINDOW = 2; // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic Blur
    private const int DWMSBT_TABBEDWINDOW = 4; // Mica Alt

    /// <summary>
    /// Habilita el efecto de desenfoque acrílico esmerilado detrás de la ventana.
    /// </summary>
    /// <param name="window">Ventana objetivo de WPF</param>
    /// <param name="alpha">Opacidad del tinte (0 a 255)</param>
    /// <param name="r">Canal rojo del tinte</param>
    /// <param name="g">Canal verde del tinte</param>
    /// <param name="b">Canal azul del tinte</param>
    public static void EnableBlur(Window window, byte alpha = 180, byte r = 16, byte g = 22, byte b = 32)
    {
        try
        {
            var handle = new WindowInteropHelper(window).EnsureHandle();
            if (handle == IntPtr.Zero) return;

            // Activar modo oscuro inmersivo en DWM si es compatible
            int darkMode = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // Si es Windows 11 build 22000+, intentar activar Backdrop Acrylic oficial
            if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22621)
            {
                int backdropAcrylic = DWMSBT_TRANSIENTWINDOW;
                int dwmResult = DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropAcrylic, sizeof(int));
                if (dwmResult == 0)
                {
                    return; // Aplicado exitosamente por DWM nativo de Windows 11
                }
            }

            // Para Windows 10 y versiones anteriores de Windows 11, usar SetWindowCompositionAttribute
            uint gradientColor = ((uint)alpha << 24) | ((uint)b << 16) | ((uint)g << 8) | (uint)r;

            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 2, // Dibuja todos los bordes correctamente
                GradientColor = gradientColor
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            int result = SetWindowCompositionAttribute(handle, ref data);
            Marshal.FreeHGlobal(accentPtr);

            // Si falla o no se soporta Acrylic, fallback a BlurBehind estándar
            if (result != 0)
            {
                var fallbackAccent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                    AccentFlags = 2,
                    GradientColor = 0
                };

                var fallbackPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(fallbackAccent, fallbackPtr, false);

                var fallbackData = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = fallbackPtr
                };

                SetWindowCompositionAttribute(handle, ref fallbackData);
                Marshal.FreeHGlobal(fallbackPtr);
            }
        }
        catch
        {
            // Silencioso en caso de ejecución en entornos sin soporte DWM o VMs sin aceleración
        }
    }
}
