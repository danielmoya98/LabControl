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
/// nativo de Windows (DWM) en ventanas flotantes de WPF.
/// </summary>
public static class WindowBlurHelper
{
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    /// <summary>
    /// Habilita el efecto de desenfoque acrílico esmerilado detrás de la ventana.
    /// </summary>
    /// <param name="window">Ventana objetivo de WPF</param>
    /// <param name="alpha">Opacidad del tinte (0 a 255)</param>
    /// <param name="r">Canal rojo del tinte</param>
    /// <param name="g">Canal verde del tinte</param>
    /// <param name="b">Canal azul del tinte</param>
    public static void EnableBlur(Window window, byte alpha = 190, byte r = 18, byte g = 24, byte b = 34)
    {
        try
        {
            var handle = new WindowInteropHelper(window).EnsureHandle();
            if (handle == IntPtr.Zero) return;

            // Formato DWM ABGR
            uint gradientColor = ((uint)alpha << 24) | ((uint)b << 16) | ((uint)g << 8) | (uint)r;

            // Intentar primero con ACCENT_ENABLE_ACRYLICBLURBEHIND (Windows 10 1803+ y Windows 11)
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

            // Si falla o no se soporta, intentar fallback a ACCENT_ENABLE_BLURBEHIND
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
