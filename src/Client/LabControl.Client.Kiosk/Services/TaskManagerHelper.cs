using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Helper para habilitar, deshabilitar y vigilar activamente el Administrador de Tareas (TaskMgr)
/// y herramientas de terminación de procesos (Process Hacker, ProcExp) en terminales de laboratorio.
/// </summary>
public static class TaskManagerHelper
{
    private const string SubKeyPolicies = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string ValueDisableTaskMgr = "DisableTaskMgr";

    private static readonly string[] ProhibitedProcessNames =
    {
        "taskmgr",
        "procexp",
        "procmon",
        "processhacker",
        "systeminformer"
    };

    private static Timer? _watchdogTimer;
    private static bool _watchdogActive;
    private static readonly object _lock = new();

    /// <summary>
    /// Deshabilita el Administrador de Tareas mediante políticas de Registro (HKCU y HKLM).
    /// </summary>
    public static bool DisableTaskManager()
    {
        bool exito = false;
        try
        {
            using var keyCu = Registry.CurrentUser.CreateSubKey(SubKeyPolicies, writable: true);
            if (keyCu != null)
            {
                keyCu.SetValue(ValueDisableTaskMgr, 1, RegistryValueKind.DWord);
                exito = true;
            }
        }
        catch { }

        try
        {
            using var keyLm = Registry.LocalMachine.CreateSubKey(SubKeyPolicies, writable: true);
            if (keyLm != null)
            {
                keyLm.SetValue(ValueDisableTaskMgr, 1, RegistryValueKind.DWord);
                exito = true;
            }
        }
        catch { }

        return exito;
    }

    /// <summary>
    /// Rehabilita el Administrador de Tareas para soporte técnico o mantenimiento.
    /// </summary>
    public static bool EnableTaskManager()
    {
        bool exito = false;
        try
        {
            using var keyCu = Registry.CurrentUser.OpenSubKey(SubKeyPolicies, writable: true);
            if (keyCu != null)
            {
                keyCu.DeleteValue(ValueDisableTaskMgr, throwOnMissingValue: false);
                exito = true;
            }
        }
        catch { }

        try
        {
            using var keyLm = Registry.LocalMachine.OpenSubKey(SubKeyPolicies, writable: true);
            if (keyLm != null)
            {
                keyLm.DeleteValue(ValueDisableTaskMgr, throwOnMissingValue: false);
                exito = true;
            }
        }
        catch { }

        return exito;
    }

    /// <summary>
    /// Termina de inmediato cualquier proceso prohibido que un estudiante intente abrir
    /// (Administrador de Tareas, Process Hacker, Process Explorer, etc.).
    /// </summary>
    public static void KillProhibitedProcesses()
    {
        foreach (var name in ProhibitedProcessNames)
        {
            try
            {
                var processes = Process.GetProcessesByName(name);
                foreach (var p in processes)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                        }
                    }
                    catch { }
                    finally
                    {
                        p.Dispose();
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Inicia el centinela en segundo plano que vigila y elimina procesos de sabotaje cada 300 ms.
    /// </summary>
    public static void StartAntiSabotageWatchdog()
    {
        lock (_lock)
        {
            if (_watchdogActive) return;
            _watchdogActive = true;

            DisableTaskManager();
            KillProhibitedProcesses();

            _watchdogTimer = new Timer(_ =>
            {
                if (!_watchdogActive) return;

                KillProhibitedProcesses();
            }, null, 0, 300);
        }
    }

    /// <summary>
    /// Detiene la vigilancia de procesos y rehabilita el Administrador de Tareas.
    /// </summary>
    public static void StopAntiSabotageWatchdog()
    {
        lock (_lock)
        {
            _watchdogActive = false;
            _watchdogTimer?.Dispose();
            _watchdogTimer = null;

            EnableTaskManager();
        }
    }
}
