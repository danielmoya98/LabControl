# 🗺️ Hoja de Ruta del Proyecto (Roadmap de Implementación)

## Sistema de Gestión, Monitoreo y Control de Acceso para Laboratorios de Cómputo (Intranet)

Este documento define el ciclo de vida, progreso e hitos técnicos del sistema para la administración centralizada de los laboratorios de cómputo universitarios (~100 PCs distribuidas en las aulas A-302, A-303 y A-308).

---

## 📊 Estado General del Proyecto

| Fase | Descripción | Estado |
| :--- | :--- | :--- |
| **Fase 1** | Fundaciones, Base de Datos, Seeder Real y CRUDs de Administración | ✅ **Completada** |
| **Fase 2** | SignalR en Tiempo Real, Telemetría de Heartbeats y Control Remoto | ✅ **Completada** |
| **Fase 3** | Refinamiento del Panel Web Administrativo (WebAdmin UX) | 🟠 **Siguiente a Implementar** |
| **Fase 4** | Blindaje y Resiliencia del Agente Cliente Kiosk (WPF) | 🔵 **Planificada** |
| **Fase 5** | Seguridad, Rendimiento y Pruebas de Carga (100 Terminales) | 🟣 **Planificada** |
| **Fase 6** | Despliegue, Empaquetado Docker y Puesta en Marcha (Producción) | 🚀 **Planificada** |

---

## ✅ Fase 1: Fundaciones, Seeder y CRUDs de Administración (Completada)

* **1.1. Base de Datos y Sembrado Real:**
  * Configuración de PostgreSQL 16 con Entity Framework Core.
  * Registro de las 3 aulas reales del Bloque Académico A: **A-302**, **A-303** y **A-308** (Capacidad fija de 26 PCs cada una).
  * Sembrado de 122 bloques horarios universitarios semanales y las 5 franjas de recreo diarias.
* **1.2. Ciclo de Vida de Sesiones:**
  * Endpoints `POST /api/sesiones/iniciar` y `POST /api/sesiones/finalizar` con validación de `@est.univalle.edu`.
  * Integración en el cliente WPF de un widget flotante translúcido con cronómetro regresivo y botón de cierre voluntario.
* **1.3. CRUDs de Administración Completos:**
  * Endpoints `PUT` y `DELETE` para Aulas, Computadoras y Horarios con integridad referencial y soft-delete.
  * Interfaz gráfica en MudBlazor WebAdmin con modales interactivos de edición y confirmación de baja.

---

## ✅ Fase 2: SignalR en Tiempo Real, Heartbeats y Control Remoto (Completada)

* **2.1. Hub Bidireccional de Comunicación (`LaboratorioHub`):**
  * Multi-canal por WebSocket: suscripciones simultáneas a canal individual de PC, canal de aula y canal global.
  * Endpoint de latidos de alta velocidad `EnviarHeartbeat` por WebSocket con respaldo HTTP REST.
* **2.2. Telemetría Continua y Monitor de Caídas:**
  * Temporizador autónomo en el cliente WPF que emite latidos cada **25 segundos**.
  * Worker en segundo plano en ASP.NET Core (`HeartbeatMonitorBackgroundService`) que marca como `Offline` cualquier terminal inactiva por más de **2 minutos**.
* **2.3. Consola de Mando Remoto:**
  * Deslogueo forzado inmediato desde el panel web (individual, por aula o masivo general) con motivo `AdminRemoto`.
  * Sistema de broadcast de alertas institucionales (`AlertaMensajeDialog.xaml`) proyectado en pantalla completa en las PCs físicas.

---

## 🟠 Fase 3: Refinamiento del Panel Web Administrativo (WebAdmin UX)

**Objetivo:** Ofrecer una interfaz visual atractiva, intuitiva y funcional para los encargados del centro de cómputo.

### 3.1. Mapa Visual de Aulas
* **Grilla gráfica interactiva con MudBlazor:**
  * 🟢 **Verde:** Terminal encendida y Disponible.
  * 🔵 **Azul:** Terminal En Uso (mostrando correo del estudiante `@est.univalle.edu`, hora de inicio y tiempo transcurrido).
  * 🔴 **Rojo:** Desconectada / Inactiva (sin señal de heartbeat > 2 minutos).
  * ⚪ **Gris:** Bloqueada administrativamente o fuera de horario académico.
* **Menú Contextual de Clic Derecho:**
  * Clic derecho sobre cualquier tarjeta de PC para desplegar menú rápido de acciones:
    * 🔒 Forzar cierre de sesión / Bloquear pantalla.
    * 💬 Enviar mensaje directo al usuario de esa terminal.
    * ✏️ Editar detalles de hardware (IP/MAC/Hostname).
    * 🔍 Ver historial de uso reciente de la terminal.

### 3.2. Módulo de Historial y Reportes
* **Tabla de Auditoría Forense:**
  * Visualización completa de todas las sesiones históricas registradas en PostgreSQL.
  * Filtros combinados dinámicos:
    * Rango de fechas (desde / hasta con selectores de calendario).
    * Filtrado por Aula (A-302, A-303, A-308 o Todas).
    * Filtrado por correo institucional de estudiante (`*@est.univalle.edu`).
    * Filtrado por Terminal física (Hostname o MAC).
    * Filtrado por Tipo de Cierre (`Manual`, `FinPeriodo`, `Recreo`, `RelevoForzado`, `AdminRemoto`).
* **Exportación de Reportes en un Clic:**
  * Generación y descarga directa de archivos **Excel (.xlsx)** y **CSV**.
  * Formato formal para presentación a jefatura académica y auditoría de uso de laboratorios.

---

## 🔵 Fase 4: Blindaje y Resiliencia del Cliente Kiosk (WPF)

**Objetivo:** Garantizar que los estudiantes no puedan saltarse el bloqueo en las terminales físicas de Windows.

### 4.1. Bloqueo a Bajo Nivel (Windows API Hooks)
* **Interceptación Global de Teclado:**
  * Hook de bajo nivel mediante `SetWindowsHookEx` (`WH_KEYBOARD_LL`).
  * Supresión de combinaciones de escape del sistema operativo:
    * `Alt + Tab` (Conmutación de tareas)
    * `Alt + F4` (Cierre forzado de ventanas)
    * `Ctrl + Esc` y Tecla Windows (Apertura del Menú Inicio)
    * `Alt + Esc` y combinaciones multimedia.
* **Restricción de Administrador de Tareas:**
  * Bloqueo de `Ctrl + Shift + Esc` y políticas de registro de Windows (`DisableTaskMgr`) mientras la pantalla de bloqueo esté activa.
* **Soporte Multi-Monitor:**
  * Renderizado `TopMost` absoluto cubriendo todas las pantallas conectadas al equipo para impedir que un usuario mueva ventanas a monitores secundarios.

### 4.2. Avisos Preventivos en Pantalla
* **Motor Local de Alertas por Horario:**
  * Sincronización de la matriz de horarios y recreos del aula correspondiente.
  * Notificaciones flotantes preventivas previas al cierre de clase o recreo:
    * **T - 10 min:** Notificación informativa amarilla (*"Quedan 10 minutos de clase. Prepare el guardado de sus trabajos"*).
    * **T - 5 min:** Notificación de precaución naranja (*"Quedan 5 minutos. Guarde sus documentos en la nube o unidad USB"*).
    * **T - 1 min:** Modal de advertencia roja con contador regresivo segundo a segundo (*"Cierre inminente en 60 segundos"*).
  * Al llegar al segundo cero, cierre automático de la sesión y bloqueo de periféricos.

### 4.3. Proceso Guardián (Watchdog)
* Agente configurado como servicio de Windows o tarea programada del sistema (`Task Scheduler` / `Registry Run`).
* Mecanismo de supervisión recíproca con auto-reinicio inmediato si el proceso WPF es finalizado de forma anómala.

---

## 🟣 Fase 5: Seguridad, Rendimiento y Pruebas de Carga

**Objetivo:** Validar que el sistema soporte ~100 computadoras concurrentes sin saturar el servidor ni la red.

### 5.1. Hardening de Seguridad
* **Autenticación Fuerte de Terminales:**
  * Firma de peticiones de telemetría y conexión a SignalR mediante API Key de máquina o Token JWT derivado de hardware (`Hostname` + `MAC`).
  * Validación en servidor para evitar que clientes no autorizados o scripts externos simulen terminales falsas.
* **Políticas Anti-DDoS y Rate Limiting:**
  * Configuración granular de limitadores de peticiones por IP/Subred en ASP.NET Core para salvaguardar la red local universitaria.

### 5.2. Simulación de Carga Masiva
* Desarrollo de un arnés de pruebas (Console Runner / k6 / NBomber) que simule **100 clientes Kiosk concurrentes**:
  * Establecimiento simultáneo de 100 sockets de SignalR.
  * Emisión periódica de latidos cada 25 segundos.
  * Simulación de 100 inicios de sesión simultáneos al arranque de un periodo de clase.
  * Medición de consumo de CPU, memoria y tiempos de respuesta en Kestrel y PostgreSQL.

### 5.3. Pruebas Automatizadas de Casos Límite
* Cobertura de pruebas unitarias y de integración para escenarios de borde:
  * Caídas transitorias de la red LAN y reconexión masiva en ráfaga.
  * Relevos forzados simultáneos entre estudiantes de clases consecutivas.
  * Deslogueo masivo de 3 aulas al unísono.

---

## 🚀 Fase 6: Despliegue, Empaquetado y Puesta en Marcha (Producción)

**Objetivo:** Desplegar en el servidor de la universidad e instalar en las aulas.

### 6.1. Servidor Centralizado (Docker Compose)
* Empaquetado completo de la infraestructura en contenedores Docker:
  * Contenedor de base de datos **PostgreSQL 16** con volúmenes de datos persistentes y scripts de backup diario automático.
  * Contenedor del **Backend Web API** con configuración para certificados SSL/TLS internos de la intranet.
  * Contenedor del **Panel WebAdmin**.
  * Orquestación con `docker-compose.yml` para inicio con un solo comando (`docker compose up -d`).

### 6.2. Instalador Desatendido para Clientes Kiosk
* Generación de paquete de instalación autónomo (`.msi` o instalador PowerShell):
  * Preconfiguración de la URL del servidor central de la intranet.
  * Detección y registro automático del equipo en el primer arranque.
  * Instalación desatendida para despliegue por clonación de discos (Clonezilla / Sysprep) o políticas de grupo Active Directory (GPO).

### 6.3. Despliegue Piloto Controlado
* **Semana 1 (Piloto Aula A-308):**
  * Instalación en las 26 terminales del aula A-308.
  * Pruebas en horarios de clases reales con docentes y estudiantes para validación ergonómica y retroalimentación.
* **Semana 2 (Despliegue General en las 3 Aulas):**
  * Extensión a las aulas A-302 y A-303 hasta alcanzar la totalidad del centro de cómputo (~100 PCs).
  * Capacitación a los administradores y personal de soporte del laboratorio.
