using Microsoft.Win32;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Helper para habilitar y deshabilitar preventivamente el Administrador de Tareas (TaskMgr)
/// en terminales Windows de laboratorios universitarios mediante políticas del Registro.
/// </summary>
public static class TaskManagerHelper
{
    private const string SubKeyPolicies = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string ValueDisableTaskMgr = "DisableTaskMgr";

    /// <summary>
    /// Deshabilita el Administrador de Tareas para la sesión de usuario actual.
    /// Si la terminal no tiene permisos suficientes, falla de forma silenciosa sin interrumpir el flujo.
    /// </summary>
    public static bool DisableTaskManager()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SubKeyPolicies, writable: true);
            if (key != null)
            {
                key.SetValue(ValueDisableTaskMgr, 1, RegistryValueKind.DWord);
                return true;
            }
        }
        catch
        {
            // Ignorar excepciones por permisos restringidos de usuario
        }
        return false;
    }

    /// <summary>
    /// Rehabilita el Administrador de Tareas para permitir soporte técnico o mantenimiento.
    /// </summary>
    public static bool EnableTaskManager()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SubKeyPolicies, writable: true);
            if (key != null)
            {
                key.DeleteValue(ValueDisableTaskMgr, throwOnMissingValue: false);
                return true;
            }
        }
        catch
        {
            // Ignorar excepciones por permisos restringidos de usuario
        }
        return false;
    }
}
