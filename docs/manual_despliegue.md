# Manual de Despliegue e Instalación en Campus — LabControl

Este documento es la guía técnica oficial para el personal de **Soporte Técnico, Infraestructura y Redes** encargado del despliegue del cliente de escritorio `LabControl.Client.Kiosk` en las terminales físicas de los laboratorios de cómputo de la **Universidad del Valle**.

---

## 📋 1. Arquitectura y Requisitos Previos

### A. Requisitos de Red
1. **Servidor Central**: Debe estar en una IP fija o nombre de dominio LAN accesible por todas las aulas (ej: `http://192.168.1.100:5256` o `http://labcontrol.univalle.edu:5256`).
2. **Puertos de Firewall**:
   * **TCP 5256 (Inbound en Servidor)**: Permite peticiones HTTP REST y conexiones bidireccionales permanentes de WebSockets (SignalR).
3. **Requisitos en Terminales Clientes (PCs de Alumnos)**:
   * Sistema Operativo: **Windows 10 (versión 1809+) o Windows 11 (64-bit)**.
   * **NO requiere .NET Runtime**: El paquete generado es **Self-Contained (Autónomo)** e incluye su propio motor de ejecución.
   * Cuenta de usuario de Windows con permisos para iniciar sesión.

---

## 📦 2. Generación del Paquete Distribuible (Build & Publish)

Para generar el instalador de producción, ejecuta el script desde la máquina de desarrollo o servidor de compilación:

```powershell
# Ubicado en la raíz del repositorio:
.\deploy\build-publish.ps1
```

Este script compila en modo Release, optimiza los binarios en un único ejecutable comprimido (`win-x64`) y genera la carpeta:
📁 `dist/LabControl-Kiosk-Client/` conteniendo:
* `LabControl.Client.Kiosk.exe` (Ejecutable autónomo con runtime .NET 10 y dependencias nativas SQLite).
* `Install-Kiosk.ps1` (Script instalador automatizado).
* `Uninstall-Kiosk.ps1` (Script de desinstalación limpia).
* `LEEME_INSTALACION.txt` (Instrucciones breves).

---

## 🚀 3. Métodos de Instalación en Laboratorios

### Método A: Instalación Semi-Asistida (Ideal para 1 a 5 PCs o prueba inicial)

1. Copia la carpeta `dist/LabControl-Kiosk-Client` a una memoria USB o a una carpeta compartida en red (`\\servidor\deploy\kiosk`).
2. En la terminal física de destino, abre la carpeta como Administrador.
3. Haz clic derecho en **`Install-Kiosk.ps1`** y selecciona **"Ejecutar con PowerShell"**.
4. El script:
   * Creará el directorio de instalación oficial: `C:\Program Files\LabControl\Kiosk\`.
   * Registrará el arranque automático en el Registro de Windows:
     `HKLM\Software\Microsoft\Windows\CurrentVersion\Run -> LabControlKiosk`.
   * Iniciará automáticamente la ventana de enrolamiento inicial:
     ![Setup Window](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/src/Client/LabControl.Client.Kiosk/SetupWindow.xaml)
5. En la ventana que aparece:
   * Ingresa la URL de la API: `http://192.168.1.100:5256` (o la IP del servidor de tu campus).
   * El sistema cargará en vivo las aulas disponibles desde la base de datos central.
   * Selecciona el **Aula** (ej. *Laboratorio de Redes - Aula 1*).
   * Verifica que el nombre de equipo detectado sea el correcto (`LAB1-PC01`, etc.).
   * Presiona **"Guardar y Bloquear Terminal"**.
6. ¡Listo! La terminal quedará inmediatamente bloqueada con el logo institucional y los hooks de protección a bajo nivel activados.

---

### Método B: Despliegue Masivo Desatendido (Para aulas de 30 a 50 PCs)

Para evitar configurar cada máquina manualmente, puedes ejecutar el script con parámetros en una sola línea mediante PowerShell remoto, PsExec o scripts de inicio de Active Directory (GPO):

```powershell
# Sintaxis:
\\servidor\deploy\kiosk\Install-Kiosk.ps1 -ApiUrl "http://192.168.1.100:5256" -AulaId 1
```

**¿Qué hace este comando de forma 100% automática?**
1. Instala los binarios en `C:\Program Files\LabControl\Kiosk`.
2. Lee automáticamente el Hostname de la máquina física (`$env:COMPUTERNAME`) y su dirección MAC real de red.
3. Genera el archivo `kiosk-config.json` con los parámetros institucionales.
4. Configura el inicio automático al encender Windows.
5. Inicia el Kiosk directamente en la pantalla de bloqueo sin requerir interacción de ningún técnico.

---

## 🛠️ 4. Procedimiento de Soporte Técnico Presencial

Si un técnico de computación necesita realizar mantenimiento en una máquina física (instalar un programa, cambiar configuración o probar periféricos):

1. En la pantalla de bloqueo, presiona la combinación de teclas:
   **`Ctrl + Shift + F12`** (o haz clic en el enlace *"🛡️ Soporte Técnico"* en el pie de página).
2. Se abrirá la ventana modal de soporte:
   * Ingresa la contraseña maestra institucional: **`AdminLab@2026`** (configurable en `kiosk-config.json`).
3. Opciones disponibles:
   * **"Desbloquear y Minimizar"**: Desactiva temporalmente los hooks de teclado y el bloqueo de Administrador de Tareas, permitiendo trabajar en Windows libremente.
   * **"Abrir Configuración de Terminal"**: Permite cambiar el aula asignada o la URL del servidor.
   * **"Salir y Cerrar Kiosk"**: Cierra limpiamente la aplicación.

---

## 🗑️ 5. Desinstalación Limpia de una Terminal

Si una PC deja de ser parte de un laboratorio o va a mantenimiento general:

1. Ejecuta PowerShell como Administrador.
2. Corre el script:
   ```powershell
   & "C:\Program Files\LabControl\Kiosk\Uninstall-Kiosk.ps1"
   ```
3. El script:
   * Cierra el proceso del Kiosk.
   * Elimina la entrada del Registro de arranque automático.
   * Restaura y rehabilita el Administrador de Tareas (`TaskMgr`) de Windows.
   * Elimina la carpeta `C:\Program Files\LabControl\Kiosk`.

---

## 🧪 6. Lista de Chequeo de Verificación Post-Instalación

| Prueba | Acción | Resultado Esperado |
| :--- | :--- | :--- |
| **Bloqueo Teclas** | Presionar `Alt+Tab`, `Tecla Windows`, `Alt+F4`, `Ctrl+Esc`. | Windows NO conmuta de ventana ni abre el menú Inicio. |
| **Arranque con Windows** | Reiniciar la computadora. | Windows arranca y antes de cualquier acción abre la pantalla de bloqueo en pantalla completa. |
| **Conexión en Vivo** | Verificar la esquina inferior izquierda de la tarjeta de login. | Muestra `● SignalR Conectado` en verde. |
| **Reflejo en WebAdmin** | Abrir `Home.razor` en el navegador. | La PC aparece en el aula con borde verde (🟢 Disponible). |
| **Inicio de Sesión** | Ingresar correo `@est.univalle.edu` y presionar Iniciar. | La pantalla de bloqueo se oculta, se activa el widget flotante con cronómetro y el teclado se libera. |
| **Modo Resiliente Offline** | Desconectar el cable de red y cerrar sesión. | La terminal se bloquea de inmediato y guarda la sesión en `labcontrol_offline.db`. Al conectar el cable, se sincroniza sola. |
