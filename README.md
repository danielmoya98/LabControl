<<<<<<< HEAD
---# 🖥️ Sistema de Gestión, Monitoreo y Control de Acceso para Laboratorios de Cómputo (Intranet)
=======
# 🖥️ LabControl — Sistema de Gestión, Monitoreo y Control de Acceso para Laboratorios de Cómputo (Intranet)
>>>>>>> 0800a435ece4f09a99e429bbd9cf33c46470b922

Sistema integral de gestión de acceso, monitoreo en tiempo real y control de sesiones para laboratorios de cómputo universitarios (**Universidad del Valle**). Diseñado para operar sobre la **Intranet Local (LAN)** administrando terminales distribuidas en aulas y laboratorios mediante mapas interactivos, control remoto por WebSockets (SignalR), detección institucional de roles por dominio (`@univalle.edu` / `@est.univalle.edu`), detección de sedes por segmento IP y gestión de horarios por períodos académicos (semestres).

---

## 🏗️ Arquitectura de la Solución (`LabControl.slnx`)

```text
LabControl.slnx
│
├── 1. SERVIDOR CENTRAL (Backend API - Clean Architecture + CQRS)
│   ├── src/Backend/LabControl.Domain/          # Entidades de dominio, Value Objects, Result<T>
│   ├── src/Backend/LabControl.Application/     # CQRS Features, MediatR Handlers, Validaciones FluentValidation
│   ├── src/Backend/LabControl.Infrastructure/  # EF Core PostgreSQL, Identity, JWT, SignalR Hub
│   └── src/Backend/LabControl.Api/             # Controladores REST, Security Headers, Middleware Serilog
│
├── 2. PANEL WEB ADMINISTRATIVO (MudBlazor WebAdmin)
│   └── src/WebAdmin/LabControl.WebAdmin/       # Blazor InteractiveServer + MudBlazor 9.9
│                                               #  (Mapa en vivo, Aulas, PCs, Asistente de Onboarding,
│                                               #   Semestres, Horarios de Docentes y Auditoría)
│
├── 3. AGENTE CLIENTE KIOSK (Terminal de Laboratorio Windows 10/11)
│   └── src/Client/LabControl.Client.Kiosk/     # WPF / .NET 10 (Modo Kiosk TopMost, Auto-Registro,
│                                               #  Detección MAC/IP/Hostname, Hooks Win32 anti-sabotaje)
│
├── 4. INSTALADOR Y DESPLIEGUE (Windows)
│   └── deploy/
│       ├── installer/                          # Script Inno Setup 6 y generador Build-Installer.ps1
│       ├── Install-Kiosk.ps1                   # Instalador automatizado PowerShell
│       └── Uninstall-Kiosk.ps1                 # Desinstalador limpio para mantenimiento
│
└── 5. SUITE DE PRUEBAS
    ├── tests/LabControl.UnitTests/             # 28 pruebas unitarias (xUnit + Moq + FluentAssertions)
    └── tests/LabControl.IntegrationTests/      # Pruebas de integración con Testcontainers PostgreSQL
```

---

## 🛠️ Stack Tecnológico

| Componente | Tecnología |
| :--- | :--- |
| **Plataforma Base** | `.NET 10.0 SDK` (C# 14 / Web API, Blazor Server y WPF) |
| **Arquitectura** | Clean Architecture + CQRS (MediatR) |
| **Base de Datos** | PostgreSQL 16+ con Entity Framework Core 10 (Npgsql) |
| **Panel Web Administrativo** | Blazor InteractiveServer + MudBlazor 9.9 (Material Design Theme) |
| **Cliente de Escritorio** | .NET 10 WPF Kiosk (Ejecutable autónomo Self-Contained x64) |
| **Instalador de Terminales** | Inno Setup 6 (Instalador Setup Wizard con arranque dual y ACLs) |
| **Motor en Tiempo Real** | ASP.NET Core SignalR (WebSockets bidireccional de baja latencia) |
| **Autenticación y Seguridad** | ASP.NET Core Identity + JWT Bearer Tokens |
| **Persistencia Local Offline** | SQLite (caché de credenciales en terminales cliente) |
| **Observabilidad** | Serilog (consola interactiva y logs rotativos en archivos) |

---

## 📋 Requisitos Previos para Instalación en Windows

Para compilar, desplegar o ejecutar la plataforma en **Windows 10, Windows 11 o Windows Server**:

1. **.NET 10.0 SDK:**  
   Descargar e instalar desde [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0).  
   *Verificar en PowerShell:* `dotnet --version` (debe retornar `10.0.xxx`).
2. **PostgreSQL 16 o superior en Windows:**  
   Descargar el instalador oficial desde [postgresql.org/download/windows](https://www.postgresql.org/download/windows/).  
   Asegurarse de que el servicio esté corriendo en el puerto **`5432`**.
3. **Herramienta opcional para compilar el instalador:**  
   [Inno Setup 6](https://jrsoftware.org/isdl.php) (solo si se desea compilar el archivo `.exe` del instalador desde el código fuente). Se puede instalar fácilmente con:
   ```powershell
   winget install JRSoftware.InnoSetup
   ```

---

## 🚀 Guía Paso a Paso de Instalación y Puesta en Marcha (Windows)

### 1️⃣ Paso 1: Configurar la Base de Datos PostgreSQL

Abre **PowerShell** (o la herramienta `psql` / pgAdmin) y ejecuta los siguientes comandos para crear la base de datos:

```powershell
# Crear la base de datos LabControlDb (ajusta la ruta de psql si tu versión difiere)
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "CREATE DATABASE \"LabControlDb\";"
```

Verifica la cadena de conexión en el archivo [`src/Backend/LabControl.Api/appsettings.json`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/src/Backend/LabControl.Api/appsettings.json):
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=LabControlDb;Username=postgres;Password=POSTGRES"
}
```
*(Cambia `POSTGRES` por la contraseña que configuraste al instalar PostgreSQL).*

Aplica las migraciones iniciales de Entity Framework Core desde la raíz del proyecto:
```powershell
dotnet ef database update --project src/Backend/LabControl.Infrastructure --startup-project src/Backend/LabControl.Api
```

> [!NOTE]
> La base de datos se inicializa en estado limpio con el rol institucional `Encargado`. Esto habilita de forma automática el **Asistente de Onboarding** en el Panel Web para registrar la primera cuenta administrativa, campus y aulas.

---

### 2️⃣ Paso 2: Iniciar el Servidor Central (Backend API)

En una terminal de PowerShell, ejecuta:

```powershell
dotnet run --project src/Backend/LabControl.Api
```

* **URL Local:** `http://localhost:5256`
* **Swagger / Documentación OpenAPI:** `http://localhost:5256/swagger`
* **Acceso desde la Red Local (LAN):**  
  Para que las terminales del laboratorio puedan conectarse a este servidor por red, asegúrate de habilitar el puerto `5256` en el Firewall de Windows:
  ```powershell
  # Ejecutar en PowerShell como Administrador:
  New-NetFirewallRule -DisplayName "LabControl API Port 5256" -Direction Inbound -LocalPort 5256 -Protocol TCP -Action Allow
  ```

---

### 3️⃣ Paso 3: Iniciar el Panel Web Administrativo (WebAdmin)

En otra terminal de PowerShell, ejecuta:

```powershell
dotnet run --project src/WebAdmin/LabControl.WebAdmin
```

Abre tu navegador web en: **`http://localhost:5077`**

#### 🌟 Asistente de Configuración Inicial (Onboarding Wizard)
Al abrir el sistema por primera vez con la base de datos nueva, el sistema redirige automáticamente al flujo guiado `/onboarding` de 5 pasos:
1. **Paso 1 - Cuenta del Encargado:** Registro de las credenciales del Administrador/Encargado de laboratorios (`@univalle.edu`).
2. **Paso 2 - Sede Universitaria:** Selección del campus (detecta y sugiere el segmento IP local, ej: `192.168.50.x` para Sucre).
3. **Paso 3 - Bloques y Aulas:** Creación de los bloques y aulas de computación del campus.
4. **Paso 4 - Período y Horarios:** Configuración del semestre académico activo y franjas horarias de laboratorios.
5. **Paso 5 - Activación:** Resumen general y activación del sistema.

Una vez completado el onboarding, tendrás acceso al panel con mapa interactivo en tiempo real, control de energía, bloqueos remotos y auditoría de sesiones.

---

### 4️⃣ Paso 4: Desplegar el Cliente Kiosk en las Terminales de Laboratorio

El cliente Kiosk se instala en las PCs físicas que usarán los estudiantes y docentes. Dispones de **3 métodos de instalación**:

#### 🔹 Método A: Instalador Asistente Setup Wizard (`.exe`) — *Recomendado*
El proyecto incluye un instalador gráfico compilado con **Inno Setup**:

* **Ruta del instalador compilado:**  
  [`dist/installer/LabControl_Kiosk_Setup_v1.0.exe`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/dist/installer/LabControl_Kiosk_Setup_v1.0.exe) (~61 MB, autónomo, no requiere preinstalar .NET en las PCs de destino).

**Pasos de instalación asistida:**
1. Copia el archivo `LabControl_Kiosk_Setup_v1.0.exe` a la PC del laboratorio (vía USB o red compartida).
2. Ejecuta el archivo con privilegios de Administrador.
3. El asistente gráfico te guiará para:
   - **Elegir la carpeta de instalación:** Por defecto `C:\Program Files\LabControl\Kiosk` (o la ruta personalizada que prefieras).
   - **Configurar la conexión institucional:** Ingresa la URL del Backend API (ej. `http://192.168.50.10:5256`) y el ID del Aula.
   - **Clave Maestra:** Por defecto `AdminLab@2026`.
4. Al finalizar, el instalador configura el inicio automático y bloquea la terminal inmediatamente.

#### 🔹 Método B: Despliegue Masivo y Silencioso (Active Directory, GPO o Scripts de Red)
Para instalar en aulas completas (30 a 50 PCs) sin intervención manual:

```powershell
# Instalación 100% silenciosa y desatendida:
LabControl_Kiosk_Setup_v1.0.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /APIURL="http://192.168.50.10:5256" /AULAID=1
```

O utilizando el script automatizado en PowerShell:
```powershell
powershell.exe -ExecutionPolicy Bypass -File deploy\Install-Kiosk.ps1 -ApiUrl "http://192.168.50.10:5256" -AulaId 1
```

#### 🔹 Método C: Compilar el Instalador Setup desde Cero
Si realizas modificaciones al código fuente del cliente Kiosk y deseas generar un nuevo instalador:

```powershell
powershell.exe -ExecutionPolicy Bypass -File deploy\installer\Build-Installer.ps1
```
*Este script compila el proyecto en Release para `win-x64`, genera los binarios autónomos y ejecuta Inno Setup para compilar el nuevo `LabControl_Kiosk_Setup_v1.0.exe`.*

---

## 🛡️ Mecanismos de Seguridad y Protección Anti-Sabotaje

El cliente Kiosk incorpora medidas de seguridad a nivel de sistema operativo para evitar que los estudiantes burlen el sistema:

| Intento de Sabotaje | Medida Técnica Implementada |
| :--- | :--- |
| **Borrar o renombrar los archivos del programa** | **Endurecimiento de Permisos NTFS (ACLs):** La carpeta `C:\Program Files\LabControl\Kiosk` queda configurada con permisos de Solo Lectura y Ejecución (`RX`) para el grupo `Usuarios` (estudiantes). Solo `Administradores` y `SYSTEM` pueden modificar o borrar archivos. |
| **Desinstalar desde el Panel de Control** | **Desinstalador Protegido por Contraseña:** El desinstalador `unins000.exe` solicita obligatoriamente la **Clave Maestra de Técnico** (`AdminLab@2026`). Sin esta clave, la desinstalación es cancelada inmediatamente. |
| **Cerrar con atajos de teclado** | **Hook Win32 de Teclado de Bajo Nivel:** Intercepta a nivel del kernel de Windows las combinaciones `Alt + F4`, `Alt + Tab`, `Tecla Windows`, `Ctrl + Esc` anulando su propagación. |
| **Abrir Administrador de Tareas** | **Directiva de Bloqueo de TaskMgr:** Aplica la política de registro `DisableTaskMgr = 1` mientras la terminal esté bloqueada. |
| **Eludir el inicio automático** | **Arranque Dual Reforzado:** Configurado en `HKLM\...\Run` y simultáneamente como **Tarea Programada de Windows** con privilegios máximos (`HIGHEST`) para que no pueda ser desmarcado desde la pestaña Inicio. |
| **Desconectar la red** | **Caché y Modo Offline SQLite:** Si la red se cae, la terminal permanece bloqueada mostrando el estado de desconexión sin liberar el escritorio de Windows. |
| **Conectar un segundo monitor o proyector** | **Bloqueador Multimonitor:** Detecta automáticamente monitores adicionales y los cubre con un lienzo negro bloqueado. |

### 🔧 Acceso a Mantenimiento Técnico Presencial
Para que un técnico de laboratorio o docente pueda desbloquear la terminal para mantenimiento:
* Atajo de teclado: **`Ctrl + Shift + Alt + F12`**
* Contraseña Maestra: **`AdminLab@2026`**
* Opciones: *Desbloquear y Minimizar*, *Configuración de Terminal*, o *Salir del Kiosk*.

---

## 🧪 Ejecución de Pruebas Unitarias

La solución cuenta con una suite completa de pruebas unitarias que validan la lógica de negocio, validaciones y reglas de autenticación:

```powershell
dotnet test tests/LabControl.UnitTests/LabControl.UnitTests.csproj
```
*(28/28 pruebas unitarias aprobadas exitosamente).*

---

## 📚 Documentación Técnica Detallada

* [`docs/GUIA_INSTALADOR_Y_SEGURIDAD_KIOSK.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/GUIA_INSTALADOR_Y_SEGURIDAD_KIOSK.md) — Guía profunda de seguridad, Inno Setup, casos borde y mitigaciones.
* [`docs/manual_despliegue.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/manual_despliegue.md) — Manual de despliegue en campus para personal de redes e infraestructura.
* [`docs/PRD.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/PRD.md) — Product Requirement Document (PRD) oficial v1.0.
* [`docs/architecture.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/architecture.md) — Especificación de arquitectura Clean Architecture, SignalR y protocolos de red.
* [`docs/roadmap.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/roadmap.md) — Hoja de ruta técnica con el desglose de fases y requerimientos.
