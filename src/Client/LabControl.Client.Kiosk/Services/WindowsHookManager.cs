using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Administrador de Low-Level Keyboard Hooks en Windows para interceptar y bloquear
/// atajos de escape del sistema (Alt+Tab, Tecla Windows, Alt+F4, Ctrl+Esc)
/// mientras la terminal se encuentra bloqueada en modo Kiosk.
/// </summary>
public static class WindowsHookManager
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    // Códigos de teclas virtuales (VK)
    private const int VK_TAB = 0x09;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_F4 = 0x73;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_SPACE = 0x20;
    private const int VK_CONTROL = 0x11;

    // Banderas en KBDLLHOOKSTRUCT
    private const int LLKHF_ALTDOWN = 0x20;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    private static IntPtr _hookId = IntPtr.Zero;
    private static readonly LowLevelKeyboardProc _proc = HookCallback;
    private static readonly object _lock = new();

    public static bool IsHookActive
    {
        get
        {
            lock (_lock)
            {
                return _hookId != IntPtr.Zero;
            }
        }
    }

    /// <summary>
    /// Instala el hook de teclado a bajo nivel para suprimir teclas de navegación y escape.
    /// </summary>
    public static void InstallHook()
    {
        lock (_lock)
        {
            if (_hookId != IntPtr.Zero) return;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            if (curModule != null)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
    }

    /// <summary>
    /// Remueve el hook de teclado restaurando el comportamiento estándar de Windows.
    /// </summary>
    public static void UninstallHook()
    {
        lock (_lock)
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
            var isAltDown = (hookStruct.flags & LLKHF_ALTDOWN) != 0;
            var isCtrlDown = (GetKeyState(VK_CONTROL) & 0x8000) != 0;

            // 1. Bloquear Tecla Windows (Izquierda y Derecha)
            if (hookStruct.vkCode == VK_LWIN || hookStruct.vkCode == VK_RWIN)
            {
                return (IntPtr)1; // Consumir evento
            }

            // 2. Bloquear Alt + Tab y Alt + Shift + Tab
            if (hookStruct.vkCode == VK_TAB && isAltDown)
            {
                return (IntPtr)1;
            }

            // 3. Bloquear Alt + Esc
            if (hookStruct.vkCode == VK_ESCAPE && isAltDown)
            {
                return (IntPtr)1;
            }

            // 4. Bloquear Ctrl + Esc (Menú de inicio alternativo)
            if (hookStruct.vkCode == VK_ESCAPE && isCtrlDown)
            {
                return (IntPtr)1;
            }

            // 5. Bloquear Alt + F4 (Evitar cerrar MainWindow)
            if (hookStruct.vkCode == VK_F4 && isAltDown)
            {
                return (IntPtr)1;
            }

            // 6. Bloquear Alt + Espacio (Menú contextual de ventana)
            if (hookStruct.vkCode == VK_SPACE && isAltDown)
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
