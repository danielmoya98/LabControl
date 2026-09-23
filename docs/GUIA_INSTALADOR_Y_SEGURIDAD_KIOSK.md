# Guía del Instalador Setup y Mitigación de Casos Borde de Seguridad (LabControl Kiosk)

Este documento detalla la arquitectura, el funcionamiento del instalador gráfico (Setup Wizard) y las medidas de seguridad anti-sabotaje implementadas para evitar que los estudiantes puedan borrar, cerrar o desinstalar el cliente Kiosk en los laboratorios de computación de la **Universidad del Valle (Univalle)**.

---

## 1. El Instalador Setup (Estilo Asistente / Wizard)

El instalador ha sido diseñado utilizando **Inno Setup 6**, el estándar de la industria en Windows (el mismo motor visual mostrado en la imagen de referencia del asistente de instalación):

* **Script oficial:** [`deploy/installer/LabControl-Kiosk-Setup.iss`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/deploy/installer/LabControl-Kiosk-Setup.iss)
* **Script de automatización:** [`deploy/installer/Build-Installer.ps1`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/deploy/installer/Build-Installer.ps1)
* **Resultado final:** Un único archivo instalador ejecutable autónomo:  
  `dist/installer/LabControl_Kiosk_Setup_v1.0.exe` (~66 MB, sin necesidad de preinstalar .NET en las máquinas de destino).

### Flujo del Asistente Gráfico durante la Instalación:
1. **Pantalla de Bienvenida:** Asistente moderno con membrete y marca Univalle.
2. **Elección de Carpeta de Destino:**  
   Por defecto sugiere `C:\Program Files\LabControl\Kiosk`, pero el técnico o encargado puede pulsar *"Examinar..."* y elegir cualquier otra unidad o directorio (ej. `D:\LabControl`, `C:\Sistemas\Kiosk`).
3. **Página de Parámetros del Servidor y Laboratorio:**  
   Paso interactivo donde el instalador solicita:
   - **URL del Servidor Central (API):** (ej: `http://192.168.50.10:5256`).
   - **Identificador / Número de Aula:** (ej: `1`).
   - **Clave Maestra de Técnico:** (por defecto `AdminLab@2026`).
   *(El instalador genera automáticamente el archivo de configuración `kiosk-config.json` en la carpeta instalada).*
4. **Instalación y Configuración del Sistema:**  
   Copia binarios, configura el arranque automático dual y aplica los permisos de seguridad.
5. **Finalización:** Opción de iniciar el Kiosk inmediatamente bloqueado al terminar.

---

## 2. Apertura Automática al Encender la PC

Para garantizar que el software se inicie inmediatamente y no pueda ser eludido al encender la computadora, se configuró un **mecanismo de arranque dual**:

1. **Clave de Inicio en el Registro de Windows:**
   - Ubicación: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
   - Clave: `"LabControlKiosk"` -> `"C:\Program Files\LabControl\Kiosk\LabControl.Client.Kiosk.exe"`
   - *Ventaja:* Inicia automáticamente para cualquier cuenta de Windows que inicie sesión.
2. **Tarea Programada de Windows (Task Scheduler con Máximos Privilegios):**
   - Comando configurado por el instalador:
     ```cmd
     schtasks /Create /TN "LabControlKioskStartup" /TR "'C:\Program Files\LabControl\Kiosk\LabControl.Client.Kiosk.exe'" /SC ONLOGON /RL HIGHEST /F
     ```
   - *Ventaja clave:* Al ejecutarse mediante `Task Scheduler` con privilegios `HIGHEST`, el sistema le otorga prioridad sobre otros programas de usuario y **no aparece en la lista simple de "Aplicaciones de Inicio" del Administrador de Tareas**, impidiendo que un estudiante desmarque su inicio.

---

## 3. Matriz de Casos Borde y Seguridad Anti-Sabotaje

A continuación se detallan los escenarios típicos de sabotaje o elusión por parte de estudiantes en laboratorios universitarios y la contramedida implementada:

| Caso Borde / Intento de Sabotaje | Riesgo | Medida Técnica Implementada |
|---|---|---|
| **1. Intentar borrar o renombrar la carpeta del programa** | El estudiante navega a `C:\Program Files\LabControl\Kiosk` e intenta presionar `Supr` o renombrar `LabControl.Client.Kiosk.exe`. | **Endurecimiento de Permisos NTFS (ACLs):** El instalador ejecuta `icacls` estableciendo permisos de Solo Lectura y Ejecución (`RX`) para el grupo `Usuarios` (estudiantes). Solo `Administradores` y `SYSTEM` tienen permisos de modificación/borrado. Windows rechaza la acción con *Acceso denegado*. |
| **2. Intentar desinstalar desde el Panel de Control** | El estudiante entra a *Configuración > Aplicaciones instaladas* o ejecuta `unins000.exe`. | **Desinstalación Protegida por Contraseña:** El desinstalador tiene programada la rutina `InitializeUninstall()`. Al ejecutarse, solicita obligatoriamente la **Clave Maestra de Técnico** (`AdminLab@2026`). Si el estudiante no la conoce o introduce una clave errónea, el desinstalador se cancela y bloquea inmediatamente. |
| **3. Intentar cerrar con combinaciones de teclas** | Presionar `Alt + F4`, `Alt + Tab`, `Tecla Windows`, `Ctrl + Esc`. | **Hook Global de Teclado de Bajo Nivel:** El cliente Kiosk intercepta mediante la API Win32 `SetWindowsHookEx(WH_KEYBOARD_LL, ...)` todas las teclas especiales y comandos de conmutación de tareas, anulando su efecto en el sistema operativo. |
| **4. Intentar abrir el Administrador de Tareas** | Presionar `Ctrl + Shift + Esc` o `Ctrl + Alt + Del`. | **Directiva de Bloqueo de TaskMgr:** Mientras la pantalla está bloqueada, se deshabilita el Administrador de Tareas mediante la política de registro `HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System\DisableTaskMgr = 1`. |
| **5. Desconectar el cable de red o apagar el Wi-Fi** | El estudiante desconecta la red para intentar que el sistema falle y lo deje pasar. | **Persistencia Local Offline (SQLite):** El Kiosk almacena la sesión y las credenciales cacheadas en `kiosk-local.db`. Si no hay red, la pantalla **permanece bloqueada** y muestra el indicador de desconexión sin liberar el escritorio. El técnico puede desbloquearlo offline con su clave. |
| **6. Conectar un segundo monitor o proyector** | El estudiante conecta una pantalla externa o cable HDMI para ver el escritorio en el monitor 2. | **Bloqueador Multimonitor Dinámico:** El Kiosk cuenta con `SecondaryMonitorBlockerWindow.xaml` que detecta la conexión de nuevos monitores y cubre instantáneamente todas las pantallas adicionales con un lienzo negro bloqueado. |
| **7. Instalación Masiva en 50+ Computadoras** | El técnico de laboratorio no quiere hacer clic manual en 50 pantallas. | **Soporte de Modo Silencioso / Desatendido:** El instalador soporta parámetros por línea de comandos para scripts o GPO: `LabControl_Kiosk_Setup_v1.0.exe /VERYSILENT /SUPPRESSMSGBOXES /DIR="C:\Program Files\LabControl\Kiosk" /APIURL="http://192.168.50.10:5256" /AULAID=1` |

---

## 4. Instrucciones para Compilar y Desplegar el Setup

### Opción A. Con Inno Setup instalado en el equipo de desarrollo:
1. Instala Inno Setup 6 (gratuito) desde [jrsoftware.org](https://jrsoftware.org/isdl.php) o ejecutando en PowerShell:
   ```powershell
   winget install JRSoftware.InnoSetup
   ```
2. Ejecuta el script automatizado:
   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File deploy\installer\Build-Installer.ps1
   ```
3. O abre directamente [`deploy/installer/LabControl-Kiosk-Setup.iss`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/deploy/installer/LabControl-Kiosk-Setup.iss) en el editor de Inno Setup y presiona **F9 (Compile)**.
4. El instalador final se generará en:
   `dist\installer\LabControl_Kiosk_Setup_v1.0.exe`

### Opción B. Despliegue Inmediato sin compilar Setup (PowerShell Elevado):
Si necesitas instalar en una máquina hoy mismo sin compilar el instalador `.exe`:
1. Copia la carpeta `dist\LabControl-Kiosk-Client` en un pendrive.
2. En la terminal de laboratorio, haz clic derecho en `Install-Kiosk.ps1` -> *"Ejecutar con PowerShell (Administrador)"*.
3. El script aplicará la instalación en `Program Files`, registrará el arranque automático y dejará la máquina asegurada.
