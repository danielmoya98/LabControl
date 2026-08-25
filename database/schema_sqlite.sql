-- ========================================================================================
-- SISTEMA DE GESTIÓN, MONITOREO Y CONTROL DE ACCESO PARA LABORATORIOS DE CÓMPUTO
-- SCRIPT DDL PARA BASE DE DATOS LOCAL OFFLINE (SQLITE)
-- Archivo objetivo: cache_offline.db (en cada PC Cliente WPF)
-- Versión: 1.0
-- ========================================================================================

PRAGMA foreign_keys = ON;

-- 1. Tabla Local: ConfigTerminal
CREATE TABLE IF NOT EXISTS ConfigTerminal (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

-- Inserción de valores por defecto de la terminal
INSERT OR IGNORE INTO ConfigTerminal (Key, Value) VALUES ('AulaID', '1');
INSERT OR IGNORE INTO ConfigTerminal (Key, Value) VALUES ('DomainAllowed', '@est.univalle.edu');
INSERT OR IGNORE INTO ConfigTerminal (Key, Value) VALUES ('ServerUrl', 'https://10.0.0.10:5001');

-- 2. Tabla Local: SesionesOffline Cache
CREATE TABLE IF NOT EXISTS SesionesOffline (
    LocalSesionID INTEGER PRIMARY KEY AUTOINCREMENT,
    EmailEstudiante TEXT NOT NULL,
    FechaHoraInicio TEXT NOT NULL, -- Formato ISO8601 UTC (YYYY-MM-DDTHH:MM:SS.SSSZ)
    FechaHoraFin TEXT,
    TipoCierre TEXT NOT NULL, -- 'Manual', 'FinPeriodo', 'Recreo', 'RelevoForzado', 'AdminRemoto'
    SyncStatus TEXT DEFAULT 'Pending' NOT NULL, -- 'Pending', 'Synced'
    FechaCreacionLocal TEXT DEFAULT (CURRENT_TIMESTAMP) NOT NULL
);

-- Índices de consulta local
CREATE INDEX IF NOT EXISTS IX_SesionesOffline_SyncStatus ON SesionesOffline(SyncStatus);
