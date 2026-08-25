-- ========================================================================================
-- SISTEMA DE GESTIÓN, MONITOREO Y CONTROL DE ACCESO PARA LABORATORIOS DE CÓMPUTO
-- SCRIPT DDL COMPLETO PARA MICROSOFT SQL SERVER
-- Versión: 1.0
-- ========================================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ControlCentrosComputo')
BEGIN
    CREATE DATABASE ControlCentrosComputo;
END
GO

USE ControlCentrosComputo;
GO

-- ========================================================================================
-- 1. TABLA: Aulas
-- ========================================================================================
IF OBJECT_ID('dbo.Aulas', 'U') IS NULL
BEGIN
    CREATE TABLE Aulas (
        AulaID INT IDENTITY(1,1) PRIMARY KEY,
        Nombre VARCHAR(50) NOT NULL,
        Capacidad INT NOT NULL,
        Pabellon VARCHAR(50) NULL,
        Activo BIT DEFAULT 1 NOT NULL
    );
END
GO

-- ========================================================================================
-- 2. TABLA: Computadoras
-- ========================================================================================
IF OBJECT_ID('dbo.Computadoras', 'U') IS NULL
BEGIN
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
END
GO

-- ========================================================================================
-- 3. TABLA: BloquesHorarios
-- ========================================================================================
IF OBJECT_ID('dbo.BloquesHorarios', 'U') IS NULL
BEGIN
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
END
GO

-- ========================================================================================
-- 4. TABLA: SesionesUso (Histórico de Auditoría)
-- ========================================================================================
IF OBJECT_ID('dbo.SesionesUso', 'U') IS NULL
BEGIN
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
END
GO

-- ========================================================================================
-- ÍNDICES DE RENDIMIENTO PARA CONSULTAS Y REPORTES
-- ========================================================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sesiones_Email' AND object_id = OBJECT_ID('SesionesUso'))
    CREATE INDEX IX_Sesiones_Email ON SesionesUso(EmailEstudiante);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sesiones_Fechas' AND object_id = OBJECT_ID('SesionesUso'))
    CREATE INDEX IX_Sesiones_Fechas ON SesionesUso(FechaHoraInicio, FechaHoraFin);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sesiones_ComputadoraID' AND object_id = OBJECT_ID('SesionesUso'))
    CREATE INDEX IX_Sesiones_ComputadoraID ON SesionesUso(ComputadoraID);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Computadoras_Aula' AND object_id = OBJECT_ID('Computadoras'))
    CREATE INDEX IX_Computadoras_Aula ON Computadoras(AulaID);
GO

-- ========================================================================================
-- VISTAS PARA REPORTES DE GESTIÓN
-- ========================================================================================

-- Vista 1: Resumen de Ocupación e Intensidad de Uso por Aula
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

-- Vista 2: Trazabilidad y Auditoría por Estudiante
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
