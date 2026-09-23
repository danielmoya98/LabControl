using FluentAssertions;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Enums;
using LabControl.Infrastructure.Services;
using Xunit;

namespace LabControl.UnitTests.Services;

public class ReportePdfServiceTests
{
    private readonly ReportePdfService _service = new();

    [Fact]
    public void GenerarReporteAsistenciaClase_ConDatos_DebeRetornarBytesValidosDeArchivoPdf()
    {
        // Arrange
        var estudiantes = new List<AsistenciaEstudianteItemDto>
        {
            new(
                NumeroPuesto: 1,
                Hostname: "LAB-A-01",
                EmailEstudiante: "juan.perez@est.univalle.edu",
                FechaHoraInicio: new DateTime(2026, 9, 21, 8, 5, 0),
                FechaHoraFin: new DateTime(2026, 9, 21, 9, 55, 0),
                DuracionMinutos: 110,
                TipoCierre: "Manual"
            ),
            new(
                NumeroPuesto: 2,
                Hostname: "LAB-A-02",
                EmailEstudiante: "maria.gomez@est.univalle.edu",
                FechaHoraInicio: new DateTime(2026, 9, 21, 8, 10, 0),
                FechaHoraFin: null,
                DuracionMinutos: 90,
                TipoCierre: "En curso"
            )
        };

        var datos = new ReporteAsistenciaClaseDto(
            SedeNombre: "Campus Central Cochabamba",
            AulaNombre: "Laboratorio Alfa - Piso 2",
            MateriaNombre: "Sistemas Operativos II",
            MateriaSigla: "SIS-302",
            DocenteNombre: "Ing. Carlos Mendoza",
            DocenteEmail: "cmendoza@univalle.edu",
            GrupoParalelo: "G-1",
            Fecha: new DateTime(2026, 9, 21),
            HoraInicioProgramada: new TimeSpan(8, 0, 0),
            HoraFinProgramada: new TimeSpan(10, 0, 0),
            CapacidadAula: 30,
            Estudiantes: estudiantes
        );

        // Act
        var bytes = _service.GenerarReporteAsistenciaClase(datos);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // Archivos PDF inician con "%PDF" (0x25, 0x50, 0x44, 0x46)
        bytes[0].Should().Be(0x25); // '%'
        bytes[1].Should().Be(0x50); // 'P'
        bytes[2].Should().Be(0x44); // 'D'
        bytes[3].Should().Be(0x46); // 'F'
    }

    [Fact]
    public void GenerarReporteAuditoria_ConDatos_DebeRetornarBytesValidosDeArchivoPdf()
    {
        // Arrange
        var sesiones = new List<SesionAuditoriaDto>
        {
            new(
                SesionId: 1,
                ComputadoraId: 1,
                Hostname: "LAB-A-01",
                AulaId: 1,
                NombreAula: "Laboratorio Alfa",
                EmailEstudiante: "juan.perez@est.univalle.edu",
                FechaHoraInicio: new DateTime(2026, 9, 20, 8, 0, 0),
                FechaHoraFin: new DateTime(2026, 9, 20, 9, 30, 0),
                DuracionMinutos: 90,
                TipoCierre: TipoCierreSesion.Manual,
                SyncStatus: SyncStatus.Online,
                FechaSincronizacion: DateTime.UtcNow
            ),
            new(
                SesionId: 2,
                ComputadoraId: 2,
                Hostname: "LAB-A-02",
                AulaId: 1,
                NombreAula: "Laboratorio Alfa",
                EmailEstudiante: "maria.gomez@est.univalle.edu",
                FechaHoraInicio: new DateTime(2026, 9, 20, 10, 0, 0),
                FechaHoraFin: new DateTime(2026, 9, 20, 11, 0, 0),
                DuracionMinutos: 60,
                TipoCierre: TipoCierreSesion.FinPeriodo,
                SyncStatus: SyncStatus.Online,
                FechaSincronizacion: DateTime.UtcNow
            )
        };

        var datos = new ReporteAuditoriaPdfDto(
            SedeNombre: "Campus Central Cochabamba",
            FechaInicio: new DateTime(2026, 9, 1),
            FechaFin: new DateTime(2026, 9, 20),
            FiltrosTexto: "Laboratorio Alfa - 01/09/2026 al 20/09/2026",
            TotalRegistros: 2,
            TotalHorasUso: 2.5,
            TotalEstudiantesUnicos: 2,
            PromedioMinutosPorSesion: 75.0,
            Sesiones: sesiones
        );

        // Act
        var bytes = _service.GenerarReporteAuditoria(datos);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);
        bytes[0].Should().Be(0x25); // '%'
        bytes[1].Should().Be(0x50); // 'P'
        bytes[2].Should().Be(0x44); // 'D'
        bytes[3].Should().Be(0x46); // 'F'
    }

    [Fact]
    public void GenerarReporteEnergia_ConDatos_DebeRetornarBytesValidosDeArchivoPdf()
    {
        // Arrange
        var infractores = new List<InfractorItemDto>
        {
            new(
                EmailEstudiante: "estudiante.reincidente@est.univalle.edu",
                NombreEstudiante: "Estudiante Reincidente",
                IncidentesCount: 1,
                TotalHorasDesperdiciadas: 3.5
            )
        };

        var incidentes = new List<RegistroEnergiaExportDto>
        {
            new(
                Id: 1,
                AulaNombre: "Laboratorio Alfa",
                Hostname: "LAB-A-05",
                UltimoEstudianteEmail: "estudiante.reincidente@est.univalle.edu",
                UltimoEstudianteNombre: "Estudiante Reincidente",
                FechaDeteccion: new DateTime(2026, 9, 20, 12, 0, 0),
                HorasInactivaEncendida: 3.5,
                MotivoInfraccion: "Equipo desatendido tras fin de clase"
            )
        };

        var datos = new ReporteEnergiaPdfDto(
            SedeNombre: "Campus Central Cochabamba",
            FechaInicio: new DateTime(2026, 9, 1),
            FechaFin: new DateTime(2026, 9, 20),
            TotalHorasDesperdiciadas: 3.5,
            TotalIncidentes: 1,
            TotalEquiposAfectados: 1,
            Incidentes: incidentes,
            TopInfractores: infractores
        );

        // Act
        var bytes = _service.GenerarReporteEnergia(datos);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);
        bytes[0].Should().Be(0x25); // '%'
        bytes[1].Should().Be(0x50); // 'P'
        bytes[2].Should().Be(0x44); // 'D'
        bytes[3].Should().Be(0x46); // 'F'
    }
}
