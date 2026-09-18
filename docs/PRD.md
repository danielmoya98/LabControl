# Product Requirement Document (PRD)

## Proyecto: Sistema de Gestión, Monitoreo y Control de Acceso para Laboratorios de Cómputo (Intranet)

* **Entorno Operativo:** Intranet Local Universitaria (Red LAN privada)
* **Alcance Físico:** 3 Aulas de Cómputo (~30–35 PCs por aula; ~100 terminales totales)
* **Dominio Permitido:** `@est.univalle.edu`
* **Stack Tecnológico Principal:** .NET 10 (WPF en Clientes, ASP.NET Core Web API + Blazor/SignalR en Panel Central, SQL Server / SQLite)
* **Versión:** 1.0 — Especificación Técnica Final para Desarrollo

---

## 1. Visión y Objetivos del Producto

### 1.1 Declaración del Problema

1. **Sesiones huérfanas y consumo energético:** Estudiantes dejan equipos encendidos con sesiones abiertas al salir a recreos o cambiar de aula.
2. **Falta de trazabilidad e identidad:** No existe un registro fidedigno de qué alumno utilizó qué máquina, en qué bloque horario y bajo qué duración.
3. **Bloqueo para relevos de clase:** Si un alumno no cerró su sesión, el siguiente estudiante de la siguiente materia encuentra dificultades o utiliza perfiles ajenos.
4. **Cero visibilidad en tiempo real:** El encargado del centro de cómputo no cuenta con un mapa en vivo de las 3 aulas ni con reportes estructurados de ocupación para auditoría académica.

### 1.2 Objetivos Clave

* Implementar un agente cliente nativo de escritorio (.NET) que bloquee la pantalla por hardware/OS hasta validar un correo institucional `@est.univalle.edu`.
* Automatizar el cierre de sesiones sincronizado con los bloques de clases y recreos del calendario universitario, notificando 10, 5 y 1 minuto antes.
* Proveer un panel web centralizado de intranet para monitoreo en vivo (SignalR), control remoto y generación de reportes (PDF/CSV/Excel).
* Garantizar alta disponibilidad con tolerancia a fallos mediante persistencia local offline (SQLite cifrado) y sincronización automática.

---

## 2. Arquitectura de Solución (Intranet Híbrida .NET)

```text
========================================================================================
                              INTRANET UNIVERSITARIA (LAN)
========================================================================================

 [ AULA 1: ~30-35 PCs ]      [ AULA 2: ~30-35 PCs ]      [ AULA 3: ~30-35 PCs ]
 +--------------------+      +--------------------+      +--------------------+
 | WPF Kiosk Client   |      | WPF Kiosk Client   |      | WPF Kiosk Client   |
 | - Local SQLite DB  |      | - Local SQLite DB  |      | - Local SQLite DB  |
 | - Keyboard Hooks   |      | - Keyboard Hooks   |      | - Keyboard Hooks   |
 +---------+----------+      +---------+----------+      +---------+----------+
           |                           |                           |
           +---------------------------+---------------------------+
                                       |
                   [ TCP / HTTPS REST & SignalR Hub ]
                   [ LAN IPs: 10.x.x.x / 192.168.x.x ]
                                       |
                                       v
         +-------------------------------------------------------------+
         |              SERVIDOR CENTRAL DE LABORATORIOS               |
         |  - ASP.NET Core Web API                                     |
         |  - SignalR Real-Time Hub (Presence, Heartbeats, Remote Cmd) |
         |  - Schedule Engine (Cron de Bloques y Recreos)              |
         |  - Entity Framework Core / SQL Server Database              |
         +-----------------------------+-------------------------------+
                                       |
                                       v
         +-------------------------------------------------------------+
         |            PANEL WEB DEL ENCARGADO (BLAZOR / SPA)           |
         |  - Mapa Interactivo de las 3 Aulas (Estados en vivo)        |
         |  - Consola de Comandos (Reinicio / Deslogueo Forzado)       |
         |  - Generador de Reportes y Auditoría (PDF / Excel)          |
         +-------------------------------------------------------------+
```

---

## 3. Actores y Matriz de Roles

| Rol | Alcance y Permisos |
| :--- | :--- |
| **Estudiante** | • Acceso interactivo en terminales físicas.<br>• Ingreso de credencial de correo institucional (`*@est.univalle.edu`).<br>• Visualización de alertas de fin de periodo.<br>• Botón manual de "Cerrar Sesión". |
| **Encargado de Laboratorio** | • Acceso completo al Panel Web Administrativo (Intranet).<br>• Monitoreo visual en tiempo real de las 3 aulas.<br>• Deslogueo forzado individual, por aula o masivo.<br>• Configuración de matrices horarias (clases, recesos, mantenimiento).<br>• Exportación y parametrización de reportes de auditoría. |

---

## 4. Requerimientos Funcionales Detallados

### 4.1 Agente Cliente (Escritorio .NET / WPF)

* **RF-01: Modo Kiosk y Bloqueo de Sistema Operativo:**
  * Debe iniciar automáticamente con el arranque del sistema (`Windows Registry Run` / `Windows Service`).
  * Intercepta atajos globales a bajo nivel mediante Windows API Hooks (`WH_KEYBOARD_LL`): `Alt+Tab`, `Alt+F4`, `Ctrl+Esc`, teclas Windows, `Ctrl+Shift+Esc`.
  * La ventana de login se renderiza en modo `TopMost` sin bordes ni barra de título, cubriendo todas las pantallas secundarias si existieran.

* **RF-02: Validación Estricta de Correo Institucional:**
  * Campo de entrada con validación de expresión regular: `^[a-zA-Z0-9._%+-]+@est\.univalle\.edu$`.
  * Si el correo es válido, desbloquea la interfaz y notifica al servidor central vía SignalR.
  * Si no coincide con el dominio, emite un aviso visual en rojo: *"Acceso restringido: Ingrese un correo @est.univalle.edu válido"*.

* **RF-03: Recolección y Telemetría de Terminal:**
  * Al iniciar sesión, el agente emite un payload de telemetría:
    * `Hostname` (ej. `LAB01-PC08`)
    * `MAC Address` física (identificador de hardware)
    * `IP Address` local asignada
    * Identificador de Aula (preconfigurado en archivo de configuración local o asignado por IP Subnet)
    * Timestamp de inicio

* **RF-04: Motor de Horarios y Notificaciones Preventivas:**
  * El agente descarga la matriz de horarios del aula asignada y corre un temporizador de precisión en segundo plano.
  * Al aproximarse el fin de bloque de clase o el recreo, levanta un overlay tipo banner flotante sobre las ventanas activas:
    * **T - 10 minutos:** Notificación informativa amarilla (*"Quedan 10 minutos de clase. Prepare el guardado de sus trabajos."*).
    * **T - 5 minutos:** Notificación de precaución naranja (*"Quedan 5 minutos. Guarde sus documentos en la nube o unidad USB."*).
    * **T - 1 minuto:** Modal con conteo regresivo en segundos (*"Cierre inminente. La sesión se bloqueará en 60 segundos."*).

* **RF-05: Cierre Automático y Relevo Forzado:**
  * Al llegar a la hora exacta de recreo o fin de clase, el agente cierra la sesión activa y levanta inmediatamente la pantalla de bloqueo.
  * Si un alumno de la siguiente clase se sienta en la máquina, puede ingresar su correo `@est.univalle.edu` directamente; el sistema registra el relevo, cierra formalmente la sesión previa (si quedó colgada) e inicia el nuevo conteo.

* **RF-06: Resiliencia y Tolerancia a Fallos de Red (Offline Mode):**
  * Si la red de la universidad se desconecta, el cliente no bloquea el uso legítimo de la clase:
    * Valida localmente el formato del correo mediante Regex.
    * Persiste la sesión en `cache_offline.db` (SQLite local cifrado).
    * Al reanudar comunicación con el servidor central, sincroniza en bloque (batch payload) todos los registros pendientes con bandera `SyncStatus = 'OfflineSync'`.

---

### 4.2 Panel Web Administrativo (ASP.NET Core & Blazor / SignalR)

* **RF-07: Tablero de Control en Tiempo Real (3 Aulas):**
  * Vista visual organizada por pestañas o grillas para Aula 1, Aula 2 y Aula 3 (cada una con sus ~30–35 PCs).
  * Tarjetas de estado con actualización instantánea por SignalR:
    * 🟢 **Verde (En Uso):** Muestra correo del alumno, hora de inicio, tiempo transcurrido y tiempo restante del bloque.
    * ⚪ **Gris (Disponible/Bloqueada):** PC encendida lista para recibir estudiante.
    * 🟡 **Amarillo (Alerta/Offline):** Sin conexión al servidor o registrando datos locales.
    * 🔴 **Rojo (Inactiva/Apagada):** Sin señal de heartbeat en los últimos 2 minutos.

* **RF-08: Control y Mando Remoto:**
  * El encargado puede seleccionar una PC individual, un aula completa o todas las terminales para:
    * Forzar cierre de sesión inmediato.
    * Enviar mensaje emergente de alerta al laboratorio (ej. *"Por favor desalojar el laboratorio en 5 minutos"*).
    * Reiniciar / Apagar equipos de forma remota al finalizar la jornada.

* **RF-09: Gestor de Horarios y Calendario Académico:**
  * Configuración flexible de turnos por aula (Lunes a Sábado).
  * Definición de horas de inicio, horas de fin, franjas de recreo y periodos de mantenimiento.

* **RF-10: Módulo de Reportería y Auditoría:**
  * Filtros dinámicos: Rango de fechas y horas, Aula, Terminal específica, Correo de estudiante, Tipo de cierre.
  * Exportación en un clic a PDF institucional formal y archivos CSV/Excel.
  * Métricas consolidadas calculadas:
    * Total de horas-máquina consumidas por aula en el mes.
    * Porcentaje de ocupación por horario y por laboratorio.
    * Historial de auditoría por alumno (trazabilidad forense ante incidentes en equipos).

---

## 5. Diseño de Base de Datos (SQL Server)

### 5.1 Script DDL (Tablas e Índices)

```sql
-- Creacion de Base de Datos
CREATE DATABASE ControlCentrosComputo;
GO
USE ControlCentrosComputo;
GO

-- 1. Tabla Aulas
CREATE TABLE Aulas (
    AulaID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL,
    Capacidad INT NOT NULL,
    Pabellon VARCHAR(50) NULL,
    Activo BIT DEFAULT 1 NOT NULL
);

-- 2. Tabla Computadoras
CREATE TABLE Computadoras (
    ComputadoraID INT IDENTITY(1,1) PRIMARY KEY,
    AulaID INT NOT NULL,
    Hostname VARCHAR(100) NOT NULL UNIQUE,
    IP_Actual VARCHAR(45) NOT NULL,
    MAC_Address VARCHAR(17) NOT NULL UNIQUE,
    EstadoActual VARCHAR(20) DEFAULT 'Disponible' NOT NULL, -- 'Disponible', 'EnUso', 'Bloqueada', 'Offline'
    UltimoHeartbeat DATETIME2 NULL,
    CONSTRAINT FK_Computadoras_Aulas FOREIGN KEY (AulaID) REFERENCES Aulas(AulaID)
);

-- 3. Tabla BloquesHorarios
CREATE TABLE BloquesHorarios (
    BloqueID INT IDENTITY(1,1) PRIMARY KEY,
    AulaID INT NOT NULL,
    DiaSemana TINYINT NOT NULL, -- 1=Lunes, 2=Martes, ..., 6=Sabado
    HoraInicio TIME(0) NOT NULL,
    HoraFin TIME(0) NOT NULL,
    EsRecreo BIT DEFAULT 0 NOT NULL,
    Descripcion VARCHAR(100) NULL,
    CONSTRAINT FK_Bloques_Aulas FOREIGN KEY (AulaID) REFERENCES Aulas(AulaID)
);

-- 4. Tabla SesionesUso (Historico de Auditoria)
CREATE TABLE SesionesUso (
    SesionID BIGINT IDENTITY(1,1) PRIMARY KEY,
    ComputadoraID INT NOT NULL,
    EmailEstudiante VARCHAR(120) NOT NULL,
    FechaHoraInicio DATETIME2 NOT NULL,
    FechaHoraFin DATETIME2 NULL,
    DuracionMinutos AS DATEDIFF(MINUTE, FechaHoraInicio, FechaHoraFin) PERSISTED,
    TipoCierre VARCHAR(30) NOT NULL, -- 'Manual', 'FinPeriodo', 'Recreo', 'RelevoForzado', 'AdminRemoto'
    SyncStatus VARCHAR(20) DEFAULT 'Online' NOT NULL, -- 'Online', 'OfflineSync'
    FechaSincronizacion DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT FK_Sesiones_Computadoras FOREIGN KEY (ComputadoraID) REFERENCES Computadoras(ComputadoraID)
);

-- Indices para Optimizacion de Consultas y Reportes
CREATE INDEX IX_Sesiones_Email ON SesionesUso(EmailEstudiante);
CREATE INDEX IX_Sesiones_Fechas ON SesionesUso(FechaHoraInicio, FechaHoraFin);
CREATE INDEX IX_Sesiones_ComputadoraID ON SesionesUso(ComputadoraID);
CREATE INDEX IX_Computadoras_Aula ON Computadoras(AulaID);
GO
```

### 5.2 Vistas para Reportes de Gestión

```sql
-- Vista 1: Resumen de Ocupacion e Intensidad de Uso por Aula
CREATE OR ALTER VIEW Vista_ReporteUsoAulas AS
SELECT 
    a.AulaID,
    a.Nombre AS AulaNombre,
    CAST(s.FechaHoraInicio AS DATE) AS Fecha,
    COUNT(s.SesionID) AS TotalSesiones,
    COUNT(DISTINCT s.EmailEstudiante) AS TotalEstudiantesUnicos,
    SUM(ISNULL(s.DuracionMinutos, 0)) AS MinutosTotalesUso,
    ROUND(SUM(ISNULL(s.DuracionMinutos, 0)) / 60.0, 2) AS HorasTotalesUso
FROM Aulas a
INNER JOIN Computadoras c ON a.AulaID = c.AulaID
INNER JOIN SesionesUso s ON c.ComputadoraID = s.ComputadoraID
GROUP BY a.AulaID, a.Nombre, CAST(s.FechaHoraInicio AS DATE);
GO

-- Vista 2: Trazabilidad y Auditoria por Estudiante
CREATE OR ALTER VIEW Vista_AuditoriaEstudiantes AS
SELECT 
    s.SesionID,
    s.EmailEstudiante,
    a.Nombre AS Aula,
    c.Hostname AS PC,
    c.IP_Actual,
    s.FechaHoraInicio,
    s.FechaHoraFin,
    s.DuracionMinutos,
    s.TipoCierre,
    s.SyncStatus
FROM SesionesUso s
INNER JOIN Computadoras c ON s.ComputadoraID = c.ComputadoraID
INNER JOIN Aulas a ON c.AulaID = a.AulaID;
GO
```

---

## 6. Requerimientos No Funcionales (RNF)

* **RNF-01 (Consumo Ligero en Terminales):** El agente cliente (.NET WPF) no debe superar 50 MB de memoria RAM en reposo ni el 1.5% de CPU, garantizando que el rendimiento para herramientas de desarrollo, IDEs o software de diseño no se vea afectado.
* **RNF-02 (Latencia de Interacción):** El proceso de validación local y desbloqueo no debe tomar más de 800 milisegundos tras presionar la tecla Enter.
* **RNF-03 (Seguridad de Tráfico en Intranet):** Aunque sea intranet, las llamadas a la API REST y el Hub de SignalR deben ir firmadas con tokens JWT de máquina o mTLS para evitar que un estudiante simule paquetes con software tipo Postman.
* **RNF-04 (Concurrencia):** El servidor central debe manejar latidos (*heartbeats*) continuos de ~120 terminales concurrentes cada 20 segundos sin saturación de hilos en Kestrel/IIS.

---

## 7. Plan de Trabajo e Hitos de Implementación (Fases 1 a 6)

Para el detalle técnico exhaustivo de cada fase y entregables, consultar [`docs/roadmap.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/roadmap.md).

| Fase | Alcance Técnico Principal | Estado |
| :--- | :--- | :--- |
| **Fase 1: Fundaciones, Seeder y CRUDs** | Modelado de dominio, base de datos PostgreSQL, seeder real con 3 aulas (A-302, A-303, A-308) y 122 horarios/recreos, y endpoints CRUD de administración. | ✅ **Completada** |
| **Fase 2: SignalR, Heartbeats y Control Remoto** | Hub multi-canal dúplex, reporte continuo de latidos (25s), monitor en segundo plano de desconexiones (>2m) y comandos remotos de deslogueo/alertas. | ✅ **Completada** |
| **Fase 3: Refinamiento del WebAdmin UX** | Grilla interactiva MudBlazor con código de colores (Verde=Disponible, Azul=En Uso, Rojo=Offline, Gris=Bloqueada), menú contextual de clic derecho y módulo de auditoría con exportación a Excel/CSV. | 🟠 **Siguiente** |
| **Fase 4: Blindaje y Resiliencia del Kiosk** | Hooks de bajo nivel Win32 (`WH_KEYBOARD_LL`) para bloquear Alt+Tab, Alt+F4, teclas Windows, bloqueo de Task Manager, pantalla TopMost multi-monitor, avisos flotantes (T-10m, T-5m, T-1m) y proceso Watchdog. | 🔵 **Planificada** |
| **Fase 5: Seguridad, Rendimiento y Carga** | Hardening de autenticación por API Key/Token de máquina, rate limiting en LAN y simulación de carga concurrente con arnés para ~100 terminales SignalR. | 🟣 **Planificada** |
| **Fase 6: Despliegue, Empaquetado y Producción** | Empaquetado en contenedores Docker Compose (PostgreSQL persistente, API HTTPS, WebAdmin), instalador desatendido (.msi/PowerShell) y despliegue piloto controlado en aulas. | 🚀 **Planificada** |

