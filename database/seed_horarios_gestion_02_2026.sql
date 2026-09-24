-- ==============================================================================
-- SCRIPT DE CARGA DE HORARIOS - GESTIÓN 02-2026 (UNIVALLE SEDE SUCRE)
-- Para ejecutar en pgAdmin (Query Tool) sobre la base de datos "LabControlDb"
-- ==============================================================================

BEGIN;

-- 1. LIMPIEZA DE REGISTROS DE PRUEBA Y AUDITORÍA
-- Orden respetando integridad referencial (claves foráneas)
DELETE FROM "RegistrosConsumoEnergia";
DELETE FROM "SesionesUso";
DELETE FROM "Computadoras";
DELETE FROM "BloquesHorarios";

-- Reiniciar contadores de identidad si corresponde
ALTER TABLE "BloquesHorarios" ALTER COLUMN "Id" RESTART WITH 1;
ALTER TABLE "Computadoras" ALTER COLUMN "Id" RESTART WITH 1;
ALTER TABLE "SesionesUso" ALTER COLUMN "Id" RESTART WITH 1;
ALTER TABLE "RegistrosConsumoEnergia" ALTER COLUMN "Id" RESTART WITH 1;

-- 2. ACTUALIZAR CAPACIDADES DE AULAS SEGÚN REPORTES OFICIALES
UPDATE "Aulas" SET "Capacidad" = 50 WHERE "Id" = 3; -- A-308
UPDATE "Aulas" SET "Capacidad" = 25 WHERE "Id" = 2; -- A-303

-- 3. INSERTAR HORARIOS OFICIALES GESTIÓN 02-2026
-- PeriodoAcademicoId = 1 (Gestión 02-2026)
-- DiaSemana: 1=Lunes, 2=Martes, 3=Miércoles, 4=Jueves, 5=Viernes, 6=Sábado

-- ------------------------------------------------------------------------------
-- AULA A-308 - LABORATORIO COMPUTACIÓN (AulaId = 3)
-- ------------------------------------------------------------------------------
-- Lunes (1)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(3, 1, 1, '07:00:00'::interval, '09:40:00'::interval, 'GAME DEVELOP', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(3, 1, 1, '09:40:00'::interval, '12:20:00'::interval, 'REALIDAD VIRTUAL Y AUMENTADA', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- Martes (2)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(3, 1, 2, '08:50:00'::interval, '10:30:00'::interval, 'DIBUJO ASISTIDO POR COMPUTADORA', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(3, 1, 2, '14:50:00'::interval, '16:30:00'::interval, 'DISEÑO ARQUITECTÓNICO COMPUTARIZADO', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- Miércoles (3)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(3, 1, 3, '07:00:00'::interval, '09:40:00'::interval, 'REALIDAD VIRTUAL Y AUMENTADA', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(3, 1, 3, '09:40:00'::interval, '12:20:00'::interval, 'ANIMACIÓN DE PROYECTOS', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- Jueves (4)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(3, 1, 4, '07:00:00'::interval, '09:40:00'::interval, 'PROGRAMACIÓN MÓVIL II', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(3, 1, 4, '09:40:00'::interval, '12:20:00'::interval, 'GAME DEVELOP', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(3, 1, 4, '14:50:00'::interval, '17:30:00'::interval, 'ANIMACIÓN DE PROYECTOS', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- Viernes (5)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(3, 1, 5, '07:00:00'::interval, '10:30:00'::interval, 'TECNOLOGÍAS EMERGENTES I', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(3, 1, 5, '10:40:00'::interval, '12:20:00'::interval, 'DIBUJO ASISTIDO POR COMPUTADORA', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(3, 1, 5, '14:50:00'::interval, '18:20:00'::interval, 'DISEÑO ARQUITECTÓNICO COMPUTARIZADO', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- ------------------------------------------------------------------------------
-- AULA A-303 - LABORATORIO COMPUTACIÓN (AulaId = 2)
-- ------------------------------------------------------------------------------
-- Lunes (1)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(2, 1, 1, '07:00:00'::interval, '08:40:00'::interval, 'ADMINISTRACIÓN ESTRATÉGICA DE PROYECTOS Y NEGOCIOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 1, '08:50:00'::interval, '10:30:00'::interval, 'JUEGO DE NEGOCIOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 1, '10:40:00'::interval, '12:20:00'::interval, 'TÉCNICAS DE NEGOCIACIÓN Y COMERCIO III', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- Martes (2)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(2, 1, 2, '07:00:00'::interval, '08:40:00'::interval, 'JUEGO DE NEGOCIOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 2, '09:40:00'::interval, '12:20:00'::interval, 'ROBÓTICA APLICADA', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 2, '14:50:00'::interval, '16:30:00'::interval, 'SISTEMAS DE INFORMACIÓN ESTRATÉGICOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 2, '16:40:00'::interval, '18:20:00'::interval, 'INTELIGENCIA DE MERCADOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 2, '18:30:00'::interval, '20:10:00'::interval, 'INVESTIGACIÓN DE OPERACIONES I', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- Miércoles (3)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(2, 1, 3, '07:00:00'::interval, '08:40:00'::interval, 'NEGOCIOS INTERNACIONALES Y ORGANIZACIONES MULTILATERALES', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 3, '09:40:00'::interval, '11:30:00'::interval, 'PROYECTO DE SISTEMAS I', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 3, '11:30:00'::interval, '13:10:00'::interval, 'ELABORACIÓN Y VALORACIÓN DE INSTRUMENTOS DE MEDICIÓN', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- Jueves (4)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(2, 1, 4, '07:00:00'::interval, '08:40:00'::interval, 'JUEGO DE NEGOCIOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 4, '08:50:00'::interval, '10:30:00'::interval, 'INTELIGENCIA DE MERCADOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 4, '10:40:00'::interval, '12:20:00'::interval, 'METODOLOGÍA DE LA INVESTIGACIÓN', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 4, '14:50:00'::interval, '16:30:00'::interval, 'TÉCNICAS DE NEGOCIACIÓN Y COMERCIO III', 'A', false, false, NOW() AT TIME ZONE 'UTC');

-- Viernes (5)
INSERT INTO "BloquesHorarios" ("AulaId", "PeriodoAcademicoId", "DiaSemana", "HoraInicio", "HoraFin", "MateriaNombreManual", "GrupoParalelo", "EsRecreo", "EsUsoLibre", "FechaCreacionUtc")
VALUES 
(2, 1, 5, '07:00:00'::interval, '08:40:00'::interval, 'ADMINISTRACIÓN ESTRATÉGICA DE PROYECTOS Y NEGOCIOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 5, '11:30:00'::interval, '12:20:00'::interval, 'REDES DE COMUNICACIÓN DE DATOS', 'A', false, false, NOW() AT TIME ZONE 'UTC'),
(2, 1, 5, '14:50:00'::interval, '16:30:00'::interval, 'INVESTIGACIÓN OPERATIVA II', 'A', false, false, NOW() AT TIME ZONE 'UTC');

COMMIT;
