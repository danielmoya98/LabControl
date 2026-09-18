# 🖥️ Sistema de Gestión, Monitoreo y Control de Acceso para Laboratorios de Cómputo (Intranet)

Sistema integral de gestión de acceso, monitoreo en tiempo real y control de sesiones para laboratorios de cómputo universitarios. Diseñado para operar sobre la **Intranet Local (LAN)** administrando ~100 terminales distribuidas en aulas de cómputo con mapas interactivos y control remoto por WebSockets (SignalR).

---

## 🏗️ Estructura Global de la Solución (`LabControl.slnx`)

```text
LabControl.slnx
│
├── 1. SERVIDOR CENTRAL (Backend API - Clean Architecture + CQRS)
│   ├── src/Backend/LabControl.Domain/          # Core puro (Entidades, Value Objects, Result<T>)
│   ├── src/Backend/LabControl.Application/     # CQRS Features, MediatR Handlers, Validaciones
│   ├── src/Backend/LabControl.Infrastructure/  # EF Core PostgreSQL, Identity, JWT, SignalR Hub
│   └── src/Backend/LabControl.Api/             # Controllers REST, Security Headers, Serilog
│
├── 2. PANEL WEB ADMINISTRATIVO (MudBlazor WebAdmin)
│   └── src/WebAdmin/LabControl.WebAdmin/       # Blazor InteractiveServer + MudBlazor (Mapa en vivo,
│                                               #  Gestión de Aulas, PCs, Horarios y Auditoría)
│
├── 3. AGENTE CLIENTE KIOSK (Escritorio para ~100 PCs)
│   └── src/Client/LabControl.Client.Kiosk/     # WPF / .NET 10 (Modo Kiosk TopMost, Auto-Registro,
│                                               #  Detección MAC/IP/Hostname nativa de Windows)
│
└── 4. SUITE DE PRUEBAS
    ├── tests/LabControl.UnitTests/             # Unit tests (xUnit + Moq + FluentAssertions)
    └── tests/LabControl.IntegrationTests/      # Integration tests (Testcontainers PostgreSQL)
```

---

## 🛠️ Stack Tecnológico

| Componente | Tecnología |
| :--- | :--- |
| **Framework Base** | `.NET 10.0 SDK` (Web API, Blazor Server y WPF) |
| **Arquitectura** | Clean Architecture + Feature-Driven Design (CQRS) |
| **Persistencia** | Entity Framework Core + PostgreSQL 16 |
| **Panel Web Admin** | Blazor InteractiveServer + MudBlazor 9.9 (Material Design Theme) |
| **Agente Cliente** | .NET 10 WPF Kiosk (Auto-Registro de Red Nativo Windows) |
| **Real-time Engine** | ASP.NET Core SignalR (WebSockets dúplex bidireccional) |
| **Autenticación** | ASP.NET Core Identity + JSON Web Tokens (JWT) |
| **Observabilidad** | Serilog (Logs estructurados en consola y archivos rotativos) |
| **Pruebas** | xUnit (8/8 unit tests pasados con 100% de éxito) |

---

## 🗄️ Guía de Creación e Inicialización de la Base de Datos

Si la base de datos `LabControlDb` no existe en tu servidor PostgreSQL, tienes **2 opciones** para crearla:

### ⚡ Opción A: Creación Automática mediante EF Core Migrations (Recomendado)

1. **Establecer la contraseña del usuario `postgres` y crear la BD vacía:**
   ```bash
   sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD 'postgres';"
   sudo -u postgres psql -c "CREATE DATABASE \"LabControlDb\";"
   ```

2. **Ejecutar la Migración de Entity Framework Core:**
   ```bash
   dotnet ef database update --project src/Backend/LabControl.Infrastructure --startup-project src/Backend/LabControl.Api
   ```
   *(Nota: Al iniciar el Backend API por primera vez con `dotnet run`, el inicializador interno `DbInitializer` creará automáticamente todas las tablas, los roles y el usuario administrador por defecto).*

---

### 📄 Opción B: Creación Manual mediante Scripts SQL

Si prefieres ejecutar el DDL manualmente sobre tu cliente PostgreSQL (`psql` o PgAdmin):
* [`database/schema_sqlserver.sql`](file:///home/daniel/.NET-LABS/database/schema_sqlserver.sql) - Script DDL completo de creación de tablas.
* [`database/schema_sqlite.sql`](file:///home/daniel/.NET-LABS/database/schema_sqlite.sql) - Script para la base de datos local del cliente Kiosk.

---

## 🚀 Guía de Instalación y Ejecución de los Proyectos

### 📋 Requisitos Previos

* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [PostgreSQL 16](https://www.postgresql.org/download/) corriendo localmente en el puerto `5432`

---

### 1️⃣ Paso 1: Ejecutar el Servidor Central (Backend API)

La API corre sobre el puerto **`http://localhost:5256`**.

```bash
dotnet run --project src/Backend/LabControl.Api
```

🔑 **Credenciales Administrativas Iniciales:**
* **Email:** `admin@univalle.edu`
* **Contraseña:** `AdminPass123!`

---

### 2️⃣ Paso 2: Ejecutar el Panel Web Administrativo (Blazor MudBlazor)

El Panel Web corre sobre el puerto **`http://localhost:5077`**.

```bash
dotnet run --project src/WebAdmin/LabControl.WebAdmin
```

Navega en tu explorador a **`http://localhost:5077`**:
* **Login:** Autentícate con `admin@univalle.edu` / `AdminPass123!`.
* **Módulos Disponibles:**
  * 📊 **Dashboard Aulas:** Mapa interactivo en tiempo real con estados de computadoras y señal SignalR en vivo.
  * 🏢 **Aulas & Labs:** Creación de Aulas (Nombre, Capacidad y Pabellón).
  * 💻 **PCs & Terminales:** Registro de computadoras físicas y filtros por laboratorio.
  * ⏰ **Horarios & Recreos:** Matriz de horarios de clases y pausas.

---

### 3️⃣ Paso 3: Ejecutar / Desplegar el Agente Cliente Kiosk (WPF)

Para probar o instalar el agente de bloqueo en las PCs de los laboratorios:

```bash
# Ejecutar localmente en desarrollo:
dotnet run --project src/Client/LabControl.Client.Kiosk

# O generar ejecutable autónomo listo para desplegar en Windows 10/11:
dotnet publish src/Client/LabControl.Client.Kiosk/LabControl.Client.Kiosk.csproj -c Release -r win-x64 --self-contained
```

---

## 📄 Documentación Técnica y Hoja de Ruta

* [`docs/roadmap.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/roadmap.md) - **Hoja de Ruta Oficial (Fases 1 a 6)** con el estado de avance técnico.
* [`docs/PRD.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/PRD.md) - Product Requirement Document oficial v1.0.
* [`docs/architecture.md`](file:///d:/PROYECTOS/LAB-CONTROL/LabControl/docs/architecture.md) - Especificación de Arquitectura de Software y flujos SignalR.
