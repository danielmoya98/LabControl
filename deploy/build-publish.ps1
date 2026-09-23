<#
.SYNOPSIS
    Compila y empaqueta el cliente Kiosk en modo autónomo (Self-Contained win-x64).
    Las terminales físicas de destino NO requieren tener .NET preinstalado.
#>

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
$ProjectPath = "$ProjectRoot\src\Client\LabControl.Client.Kiosk\LabControl.Client.Kiosk.csproj"
$OutputDir = "$ProjectRoot\dist\LabControl-Kiosk-Client"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  LabControl - Compilación y Empaquetado Autónomo de Kiosk  " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Directorio Raíz: $ProjectRoot"
Write-Host "Destino:         $OutputDir"
Write-Host ""

# 1. Limpieza de salida previa
if (Test-Path $OutputDir) {
    Write-Host "[1/3] Limpiando carpeta de distribución previa..." -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force
}

# 2. Publicación con dotnet
Write-Host "[2/3] Compilando binarios autónomos (win-x64, Self-Contained)..." -ForegroundColor Yellow

$publishArgs = @(
    "publish",
    $ProjectPath,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-o", $OutputDir
)

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Fallo en la publicación con dotnet. Código de salida: $LASTEXITCODE"
    exit $LASTEXITCODE
}

# 3. Copiar scripts de instalación y centinela Guardian a la carpeta distribuible
Write-Host "[3/3] Copiando instaladores y binario centinela a la carpeta de distribución..." -ForegroundColor Yellow
Copy-Item -Path "$OutputDir\LabControl.Client.Kiosk.exe" -Destination "$OutputDir\LabControl.Guardian.exe" -Force
Copy-Item -Path "$PSScriptRoot\Install-Kiosk.ps1" -Destination $OutputDir -Force
Copy-Item -Path "$PSScriptRoot\Uninstall-Kiosk.ps1" -Destination $OutputDir -Force

# Crear archivo de instrucciones README en la distribución
$DistReadme = @"
============================================================
  LabControl Kiosk Client - Paquete de Instalación en Campus
============================================================

Este paquete contiene el cliente autónomo de bloqueo y control
de laboratorios de computación de la Universidad del Valle.

INSTRUCCIONES DE INSTALACIÓN:
1. Haz clic derecho en 'Install-Kiosk.ps1' y selecciona 'Ejecutar con PowerShell'
   (debe ejecutarse con permisos de Administrador).
2. Se abrirá automáticamente la ventana de configuración:
   - Introduce la URL del servidor central (ej: http://192.168.1.100:5256).
   - Selecciona el aula a la que pertenece este equipo.
   - Presiona 'Guardar y Bloquear'.
3. Listo. El equipo iniciará automáticamente bloqueado en cada reinicio.

INSTRUCCIONES DE DESINSTALACIÓN:
- Haz clic derecho en 'Uninstall-Kiosk.ps1' y selecciona 'Ejecutar con PowerShell'.
"@

Set-Content -Path "$OutputDir\LEEME_INSTALACION.txt" -Value $DistReadme -Encoding UTF8

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  ¡EMPAQUETADO COMPLETADO CON ÉXITO!                       " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Archivos generados listos para distribución en:"
Write-Host "  $OutputDir" -ForegroundColor White
Write-Host ""
