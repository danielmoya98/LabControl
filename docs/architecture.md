# Especificación de Arquitectura de Software

## Sistema de Gestión, Monitoreo y Control de Acceso para Laboratorios de Cómputo (Intranet)

---

## 1. Visión General de la Arquitectura

El sistema adopta un modelo de **Arquitectura Híbrida Distribuida** optimizado para redes de área local (LAN) privadas. Consta de tres capas principales:

1. **Capa Agente Cliente (Desktop App - Terminales de Aula):**
   * Aplicación nativa .NET (WPF) instalada en cada una de las ~100 terminales (~30–35 PCs por aula).
   * Ejecutada como servicio o aplicación en el inicio del sistema operativo (`Kiosk Mode`).
   * Implementa ganchos del sistema operativo a bajo nivel (`Windows API WH_KEYBOARD_LL`) para bloquear combinaciones de teclas destructivas o evasivas.
   * Cuenta con un almacenamiento local ultraligero (`SQLite` cifrado) para resiliencia frente a caídas de red.

2. **Capa Servidor Backend (Intranet Server):**
   * API REST en ASP.NET Core Web API encargada de la lógica de negocio, autenticación, reportería y sincronización.
   * **SignalR Real-Time Hub:** Mantiene una conexión WebSocket/Server-Sent Events dúplex persistente con las 100 terminales para recepcionar *heartbeats*, enviar comandos remotos en tiempo real y notificar cambios de estado.
   * **Motor de Horarios (Schedule Engine):** Tarea programada en segundo plano (*BackgroundService*) para evaluar bloques de clase y períodos de receso.

3. **Capa Panel de Administración (Web UI):**
   * Aplicación web desarrollada en **Blazor** (Server o WebAssembly).
   * Consume el Hub de SignalR para renderizar el mapa interactivo de las 3 aulas con refresco instantáneo sin necesidad de recargar la página.
   * Generación de reportes dinámicos en PDF, Excel y CSV mediante motores de renderizado en servidor.

---

## 2. Diagrama de Flujo de Comunicaciones e Interacción

```text
  [ Terminal Client (WPF) ]               [ Servidor Intranet (SignalR Hub) ]         [ Panel Web Encargado (Blazor) ]
             |                                              |                                        |
 1. Inicio PC| -- Register/Connect WebSocket -------------> |                                        |
             | <-- Acknowledge & Send Horarios ----------- |                                        |
             |                                              | -- Notificar Estado 'Disponible' ----> |
             |                                              |                                        |
 2. Login    | -- Validar @est.univalle.edu (Regex) -------> |                                        |
    Estudiante| -- Evento 'SesionIniciada' -----------------> | -- Actualizar Tarjeta PC a 'En Uso' --> |
             |                                              |                                        |
 3. Heartbeat| -- Pulse cada 20s (IP, MAC, Hostname) ------> | -- Mantener Estado 'Alive' -----------> |
             |                                              |                                        |
 4. Alerta   | <-- Push Notificación (T-10m, T-5m, T-1m) --- | (Iniciado por Cron o Admin)            |
    Cierre   | -- Bloquear & Cierre de Sesión ------------> | -- Actualizar Estado a 'Disponible' --> |
             |                                              |                                        |
 5. Cmd Admin| <-------------------------------------------- | <-- Cierre Forzado / Mensaje / Apagar |
```

---

## 3. Estrategia de Bloqueo a Bajo Nivel (Kiosk Agent)

El agente WPF intercepta atajos de teclado del sistema mediante la API de Windows `SetWindowsHookEx` con la constante `WH_KEYBOARD_LL` (13):

* **Teclas Interceptadas:**
  * `Alt + Tab` (Cambio de ventana)
  * `Alt + F4` (Cierre de ventana)
  * `Ctrl + Esc` / `Win Key` (Menú Inicio)
  * `Ctrl + Shift + Esc` (Administrador de Tareas)
* **Comportamiento:**
  * La ventana de Login corre con propiedad `TopMost = true`, `WindowState = Maximized` y `WindowStyle = None`.
  * Desactiva el explorador si no hay correo validado (`^[a-zA-Z0-9._%+-]+@est\.univalle\.edu$`).

---

## 4. Resiliencia y Modo Offline (`Offline First`)

Cuando la conexión LAN entre el cliente WPF y el Servidor Central se interrumpe:

1. **Detección:** El cliente detecta la pérdida del socket de SignalR tras 3 reintentos de Heartbeat fallidos.
2. **Operación Offline:** El estudiante puede ingresar su correo `@est.univalle.edu`. El agente valida el formato por Expresión Regular localmente y desbloquea el equipo.
3. **Persistencia Local:** La sesión se registra en la base de datos local `cache_offline.db` (SQLite cifrado con SQLCipher / DPAPI) con la bandera `SyncStatus = 'OfflineSync'`.
4. **Reconexión y Sincronización Batch:** Al restaurar la conexión con el servidor central, un hilo en segundo plano envía un payload de sincronización por lotes (`POST /api/sessions/sync-batch`). El servidor procesa la lista y actualiza los registros en SQL Server marcando la fecha real de sincronización (`FechaSincronizacion = UTC`).

---

## 5. Matriz de Seguridad en Intranet

* **Tokens JWT de Máquina:** Cada terminal cliente se autentica inicialmente con una clave de máquina/certificados mTLS o API Key fija asignada al par `Hostname` + `MAC_Address`.
* **Cifrado de Base de Datos Local:** La base de datos SQLite cliente está protegida mediante llaves de cifrado derivadas de la DPAPI de Windows para evitar que estudiantes editen la base de datos local manualmente.
* **Canal Seguro:** Comunicaciones vía HTTPS REST y WSS (WebSockets sobre TLS) dentro de la red privada LAN.
