<#
.SYNOPSIS
    Desinstalador limpio del Cliente Kiosk de LabControl.
.DESCRIPTION
    Cierra procesos activos, rehabilita el Administrador de Tareas de Windows,
    elimina el autostart del registro y borra los archivos del sistema.
#>

# 1. Verificar y elevar permisos de Administrador
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Elevando permisos de administrador..." -ForegroundColor Yellow
    Start-Process powershell.exe -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"") -Verb RunAs
    exit
}

$InstallDir = "$env:ProgramFiles\LabControl\Kiosk"
$RegistryPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Run"
$RegistryName = "LabControlKiosk"

Write-Host "============================================================" -ForegroundColor Red
Write-Host "     LabControl Kiosk - Desinstalación de Terminal          " -ForegroundColor Red
Write-Host "============================================================" -ForegroundColor Red
Write-Host ""

# 2. Detener procesos del Kiosk
Write-Host "[1/4] Cerrando cliente Kiosk..." -ForegroundColor Yellow
Get-Process -Name "LabControl.Client.Kiosk" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# 3. Eliminar autostart del registro
Write-Host "[2/4] Removiendo arranque automático de Windows..." -ForegroundColor Yellow
if (Get-ItemProperty -Path $RegistryPath -Name $RegistryName -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path $RegistryPath -Name $RegistryName -Force
}

# 4. Rehabilitar el Administrador de Tareas en las políticas del sistema
Write-Host "[3/4] Rehabilitando el Administrador de Tareas de Windows..." -ForegroundColor Yellow
$policiesPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\System"
if (Test-Path $policiesPath) {
    Remove-ItemProperty -Path $policiesPath -Name "DisableTaskMgr" -ErrorAction SilentlyContinue
}

# 5. Eliminar archivos de instalación
Write-Host "[4/4] Eliminando archivos instalados..." -ForegroundColor Yellow
if (Test-Path $InstallDir) {
    Remove-Item -Path $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  ¡DESINSTALACIÓN COMPLETADA!                              " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "La terminal física ha sido restaurada a su estado estándar."
Write-Host ""
Start-Sleep -Seconds 3
