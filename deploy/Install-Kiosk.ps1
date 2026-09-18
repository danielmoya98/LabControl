<#
.SYNOPSIS
    Instalador automatizado del Cliente Kiosk de LabControl para terminales físicas con Windows.
.DESCRIPTION
    Instala los binarios en Program Files, configura el arranque automático en el Registro de Windows
    e inicia la aplicación para el enrolamiento guiado en el laboratorio.
.PARAMETER ApiUrl
    URL opcional del servidor central (ej: http://192.168.1.100:5256) para instalación desatendida.
.PARAMETER AulaId
    Identificador del aula (entero) para instalación desatendida.
#>

param(
    [string]$ApiUrl = "",
    [int]$AulaId = 0
)

# 1. Verificar y elevar permisos de Administrador
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Elevando permisos de administrador..." -ForegroundColor Yellow
    Start-Process powershell.exe -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" $(if ($ApiUrl) { "-ApiUrl `"$ApiUrl`"" }) $(if ($AulaId -gt 0) { "-AulaId $AulaId" })") -Verb RunAs
    exit
}

$SourceDir = $PSScriptRoot
$InstallDir = "$env:ProgramFiles\LabControl\Kiosk"
$TargetExe = "$InstallDir\LabControl.Client.Kiosk.exe"
$RegistryPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Run"
$RegistryName = "LabControlKiosk"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "      LabControl Kiosk - Instalador de Terminal Física      " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Directorio Origen:  $SourceDir"
Write-Host "Destino Instalado:  $InstallDir"
Write-Host ""

# 2. Detener proceso si está activo
Write-Host "[1/4] Verificando procesos activos..." -ForegroundColor Yellow
Get-Process -Name "LabControl.Client.Kiosk" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# 3. Crear directorio y copiar archivos
Write-Host "[2/4] Instalando archivos del cliente Kiosk..." -ForegroundColor Yellow
if (-not (Test-Path $InstallDir)) {
    New-Item -Path $InstallDir -ItemType Directory -Force | Out-Null
}

Get-ChildItem -Path $SourceDir -Exclude "Install-Kiosk.ps1", "Uninstall-Kiosk.ps1" | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $InstallDir -Recurse -Force
}

# 4. Configurar Autostart con Windows en el Registro
Write-Host "[3/4] Configurando arranque automático al encender Windows..." -ForegroundColor Yellow
Set-ItemProperty -Path $RegistryPath -Name $RegistryName -Value "`"$TargetExe`"" -Force

# 5. Configuración desatendida opcional (si se pasaron parámetros)
if ($ApiUrl -and $AulaId -gt 0) {
    Write-Host "Aplicando configuración desatendida..." -ForegroundColor Yellow
    $hostname = $env:COMPUTERNAME
    $mac = (Get-NetAdapter | Where-Object { $_.Status -eq 'Up' -and $_.HardwareInterface } | Select-Object -First 1).MacAddress
    if (-not $mac) { $mac = "00:00:00:00:00:00" }

    $configObj = @{
        ApiBaseUrl    = $ApiUrl
        AulaId        = $AulaId
        AulaNombre    = "Aula $AulaId"
        ComputadoraId = 0
        Hostname      = $hostname
        MacAddress    = $mac
        ClaveTecnico  = "AdminLab@2026"
    }

    $configJson = $configObj | ConvertTo-Json -Depth 2
    Set-Content -Path "$InstallDir\kiosk-config.json" -Value $configJson -Encoding UTF8
    Write-Host "Configuración pre-establecida para $hostname en Aula $AulaId." -ForegroundColor Green
}

# 6. Lanzar la aplicación
Write-Host "[4/4] Iniciando cliente Kiosk de LabControl..." -ForegroundColor Yellow
Start-Process -FilePath $TargetExe -WorkingDirectory $InstallDir

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  ¡INSTALACIÓN COMPLETADA EXITOSAMENTE!                     " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "La terminal física iniciará bloqueada automáticamente con Windows."
Write-Host ""
Start-Sleep -Seconds 3
