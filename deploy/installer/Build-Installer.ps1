<#
.SYNOPSIS
    Compilador automatizado del instalador Setup (.exe) para LabControl Kiosk.
.DESCRIPTION
    1. Verifica/compila los binarios autónomos (Self-Contained) de LabControl.Client.Kiosk.
    2. Localiza o descarga el compilador Inno Setup 6 (iscc.exe).
    3. Compila el script LabControl-Kiosk-Setup.iss generando el instalador final listo para distribución.
#>

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path "$PSScriptRoot\..\.."
$DistDir = "$ProjectRoot\dist\LabControl-Kiosk-Client"
$InstallerScript = "$PSScriptRoot\LabControl-Kiosk-Setup.iss"
$OutputDir = "$ProjectRoot\dist\installer"
$TargetExe = "$DistDir\LabControl.Client.Kiosk.exe"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "      LabControl Kiosk - Generador del Instalador Setup     " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Directorio Raíz:      $ProjectRoot"
Write-Host "Script Inno Setup:    $InstallerScript"
Write-Host "Destino del Setup:    $OutputDir"
Write-Host ""

# 1. Asegurar que los binarios autónomos estén compilados
if (-not (Test-Path $TargetExe)) {
    Write-Host "[1/3] Binarios no encontrados. Compilando cliente Kiosk con dotnet publish..." -ForegroundColor Yellow
    & "$ProjectRoot\deploy\build-publish.ps1"
} else {
    Write-Host "[1/3] Binarios autónomos encontrados en $DistDir." -ForegroundColor Green
}

# 2. Localizar Inno Setup Compiler (iscc.exe)
Write-Host "[2/3] Buscando Inno Setup Compiler (iscc.exe)..." -ForegroundColor Yellow

$isccPath = $null
$possiblePaths = @(
    "iscc.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe",
    "$env:ProgramFiles\Inno Setup 6\iscc.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\iscc.exe",
    "C:\InnoSetup6\iscc.exe"
)

foreach ($path in $possiblePaths) {
    if (Get-Command $path -ErrorAction SilentlyContinue) {
        $isccPath = (Get-Command $path).Source
        break
    }
    if (Test-Path $path) {
        $isccPath = $path
        break
    }
}

# 3. Compilar el archivo .iss
if ($isccPath) {
    Write-Host "Compilador Inno Setup encontrado: $isccPath" -ForegroundColor Green
    Write-Host "[3/3] Generando instalador ejecutable LabControl_Kiosk_Setup_v1.0.exe..." -ForegroundColor Yellow

    if (-not (Test-Path $OutputDir)) {
        New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
    }

    & "$isccPath" "$InstallerScript"

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "============================================================" -ForegroundColor Green
        Write-Host "  ¡INSTALADOR SETUP GENERADO CON ÉXITO!                     " -ForegroundColor Green
        Write-Host "============================================================" -ForegroundColor Green
        Write-Host "Ejecutable listo para instalar en terminales de laboratorio:"
        Write-Host "  $OutputDir\LabControl_Kiosk_Setup_v1.0.exe" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Error "Error durante la compilación del instalador Inno Setup. Código: $LASTEXITCODE"
    }
} else {
    Write-Host ""
    Write-Host "------------------------------------------------------------" -ForegroundColor Yellow
    Write-Host "AVISO: Inno Setup 6 no está instalado en este equipo." -ForegroundColor Yellow
    Write-Host "------------------------------------------------------------" -ForegroundColor Yellow
    Write-Host "El script de Inno Setup está listo y validado en:"
    Write-Host "  $InstallerScript" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Para compilar el .exe instalador en 1 minuto:"
    Write-Host "1. Descarga Inno Setup 6 (gratuito) desde: https://jrsoftware.org/isdl.php"
    Write-Host "2. O instálalo desde consola con: winget install JRSoftware.InnoSetup"
    Write-Host "3. Abre 'deploy\installer\LabControl-Kiosk-Setup.iss' y presiona 'F9' (Compile)."
    Write-Host "   O vuelve a ejecutar este script: .\deploy\installer\Build-Installer.ps1"
    Write-Host "------------------------------------------------------------" -ForegroundColor Yellow
    Write-Host ""
}
