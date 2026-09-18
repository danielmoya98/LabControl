# Especificación Técnica de Mejoras para Futuros Releases — LabControl

Este documento detalla la arquitectura, modelos de datos, endpoints y casos de uso para las funcionalidades avanzadas sugeridas para las próximas versiones (v2.0+) de **LabControl**.

---

## 1. Módulo de Inventario de Hardware y Diagnóstico Técnico (Activos Fijos)

### Objetivo
Permitir a la Dirección de TI y jefatura de laboratorios conocer en tiempo real la ficha técnica exacta de cada terminal del campus sin necesidad de auditorías físicas manuales.

### Datos a Recolectar (Extracción vía WMI / System.Management en Kiosk)
| Parámetro | Clase WMI / API | Ejemplo de Valor | Utilidad en la Universidad |
| :--- | :--- | :--- | :--- |
| **Modelo de Procesador** | `Win32_Processor` -> `Name` | `Intel(R) Core(TM) i7-12700K` | Planificación académica para materias de renderizado / IA. |
| **Núcleos / Hilos** | `Win32_Processor` -> `NumberOfCores` | `12 Cores / 20 Threads` | Capacidad de paralelismo y virtualización. |
| **Memoria RAM Total** | `Win32_ComputerSystem` -> `TotalPhysicalMemory` | `16 GB DDR4` | Verificación de requerimientos mínimos de software. |
| **Almacenamiento Principal** | `Win32_DiskDrive` & `DriveInfo` | `SSD Kingston NVMe 500GB (Libre: 38 GB)` | **Crítico**: Prevenir congelamientos de Windows por disco lleno. |
| **Tarjeta Gráfica (GPU)** | `Win32_VideoController` -> `Name` | `NVIDIA GeForce RTX 3060 12GB` | Asignación de aulas para Diseño, Arquitectura y Videojuegos. |
| **Resolución de Pantalla** | `SystemParameters.PrimaryScreenWidth/Height` | `1920 x 1080 @ 60Hz` | Verificación de estándares ergonómicos. |
| **Serial Number (BIOS)** | `Win32_BIOS` -> `SerialNumber` | `R90XYZ123` | Código de activo fijo patrimonial de la universidad. |
| **Versión de Windows** | `Win32_OperatingSystem` -> `Caption`, `BuildNumber` | `Windows 11 Pro 64-bit (22631)` | Control de actualizaciones y compatibilidad. |
| **Tiempo de Actividad (Uptime)**| `Environment.TickCount64` | `4 días, 8 horas` | Detección de terminales olvidadas encendidas. |
| **Versión del Agente Kiosk** | `Assembly.GetExecutingAssembly().Version` | `v1.4.0` | Auditoría de versiones desplegadas en campus. |

### Modelo de Base de Datos Propuesto
```sql
ALTER TABLE "Computadoras" 
ADD COLUMN "CpuModelo" VARCHAR(150),
ADD COLUMN "RamTotalGb" INT,
ADD COLUMN "DiscoTotalGb" INT,
ADD COLUMN "DiscoLibreGb" INT,
ADD COLUMN "GpuModelo" VARCHAR(150),
ADD COLUMN "NumeroSerieBios" VARCHAR(100),
ADD COLUMN "SistemaOperativo" VARCHAR(120),
ADD COLUMN "KioskVersion" VARCHAR(20),
ADD COLUMN "UptimeHoras" INT;
```

---

## 2. Telemetría de Salud en Tiempo Real (Heartbeat Enriquecido)

### Flujo de Funcionamiento
En cada latido enviado por WebSocket (`EnviarHeartbeat` cada 25s), el cliente Kiosk adjunta un payload ligero:
```json
{
  "hostname": "LAB1-PC08",
  "ip": "192.168.1.108",
  "cpuUsoPorcentaje": 24,
  "ramUsoPorcentaje": 58,
  "discoLibreGb": 14.5
}
```

### Reglas de Alerta en WebAdmin
* 🟡 **Alerta de Almacenamiento**: Si `discoLibreGb < 10`, la tarjeta de la PC muestra un badge amarillo: `⚠️ Poco espacio en C: (8.2 GB)`.
* 🔴 **Alerta de Sobrecarga**: Si `cpuUsoPorcentaje > 95%` durante más de 3 latidos consecutivos, badge rojo: `🔥 CPU Saturada (98%)` para advertir sobre procesos colgados o minería no autorizada.

---

## 3. Comandos de Energía Remota (Power Management)

### A. Apagado y Reinicio Remoto
* **Apagado Masivo Nocturno**: Botón en la cabecera del WebAdmin para apagar todas las PCs de un aula o del campus a la hora de cierre institucional (ej: 22:00) vía SignalR:
  ```csharp
  // Kiosk Client ejecuta:
  Process.Start("shutdown.exe", "/s /t 10 /c \"Apagado institucional programado por jefatura de cómputo.\"");
  ```
* **Reinicio de Aula**: Útil para aulas con software de congelamiento de disco (Deep Freeze / Shadow Defender) para restablecer perfiles limpios entre clases:
  ```csharp
  Process.Start("shutdown.exe", "/r /t 5 /f");
  ```

### B. Wake-on-LAN (Encendido Remoto por Red)
* El servidor backend emite paquetes mágicos UDP a la dirección broadcast (`255.255.255.255:9`) con la dirección MAC de la PC seleccionada:
  ```csharp
  public async Task EnviarWakeOnLanAsync(string macAddress) { ... }
  ```
* Permite encender aulas completas 10 minutos antes de que empiece el turno de la mañana.

---

## 4. Módulo de Exámenes Seguros (Exam Shield)

### Objetivo
Convertir las terminales físicas en un entorno seguro anti-fraude durante exámenes parciales y finales.

### Características del Modo Examen
1. **Lista Blanca de URLs**: La terminal solo permite navegar al Moodle institucional (`aulavirtual.univalle.edu`), bloqueando Google, ChatGPT, foros, etc.
2. **Bloqueo de Puertos USB**: Deshabilitación de dispositivos de almacenamiento extraíble (pendrives) durante el examen.
3. **Inhabilitación de Portapapeles Externo**: Limpieza de portapapeles entre cambios de foco.
4. **Alerta de Pérdida de Foco**: Si el estudiante minimiza o intenta abrir otra ventana, el sistema envía una alerta al docente en el WebAdmin: `"Estudiante X intentó salir de la ventana del examen"`.

---

## 5. Supervisión Visual en Miniatura (Thumbnail Screen Monitoring)

### Objetivo
Permitir a los docentes y encargados supervisar visualmente las pantallas de los estudiantes en tiempo real desde el navegador.

### Mecanismo de Implementación
* A petición del WebAdmin (o cada 15 segundos en modo supervisión), el cliente Kiosk captura la pantalla principal:
  ```csharp
  using var bmp = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
  // Redimensionar a miniatura 320x180 px con calidad JPEG 60% (~15 KB por frame)
  ```
* Se transmite por SignalR o canal WebRTC hacia el WebAdmin.
* En el mapa de aulas de `Home.razor`, las tarjetas pueden alternar entre vista de estado o **grilla de pantallas en vivo**.

---

## 6. Canal de Asistencia Estudiante ↔ Encargado (Help Desk)

### Flujo de Soporte
* En el widget flotante del estudiante (`SessionWidgetWindow`), se incorpora un botón: **"🙋 Solicitar Soporte"**.
* Opciones rápidas:
  * *"Mouse o teclado no funcionan"*
  * *"No tengo acceso a Internet"*
  * *"Problema con software de la clase"*
* Al enviar la solicitud, la tarjeta de la PC en el panel WebAdmin parpadea con una notificación audible y visual:
  `"🔔 Solicitud de asistencia en LAB2-PC14: Problema de internet"`.
* El encargado puede responder con un mensaje directo: `"Un técnico va en camino"`.
