using System.Text;
using FluentAssertions;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Enums;
using LabControl.Infrastructure.Services;
using Xunit;

namespace LabControl.UnitTests.Services;

public class ReporteExcelServiceTests
{
    private readonly ReporteExcelService _service = new();

    private List<SesionAuditoriaDto> ObtenerSesionesPrueba()
    {
        return new List<SesionAuditoriaDto>
        {
            new(
                SesionId: 101,
                ComputadoraId: 1,
                Hostname: "LAB1-PC01",
                AulaId: 1,
                NombreAula: "Laboratorio de Redes",
                EmailEstudiante: "estudiante.test@est.univalle.edu",
                FechaHoraInicio: new DateTime(2026, 9, 15, 8, 0, 0),
                FechaHoraFin: new DateTime(2026, 9, 15, 9, 30, 0),
                DuracionMinutos: 90,
                TipoCierre: TipoCierreSesion.Manual,
                SyncStatus: SyncStatus.Online,
                FechaSincronizacion: new DateTime(2026, 9, 15, 9, 30, 5)
            ),
            new(
                SesionId: 102,
                ComputadoraId: 2,
                Hostname: "LAB1-PC02",
                AulaId: 1,
                NombreAula: "Laboratorio de Redes",
                EmailEstudiante: "maria.perez@est.univalle.edu",
                FechaHoraInicio: new DateTime(2026, 9, 15, 10, 0, 0),
                FechaHoraFin: new DateTime(2026, 9, 15, 11, 0, 0),
                DuracionMinutos: 60,
                TipoCierre: TipoCierreSesion.FinPeriodo,
                SyncStatus: SyncStatus.OfflineSync,
                FechaSincronizacion: new DateTime(2026, 9, 15, 11, 5, 0)
            )
        };
    }

    [Fact]
    public void GenerarReporteAuditoriaExcel_ConDatos_DebeRetornarBytesValidosDeArchivoExcel()
    {
        // Arrange
        var sesiones = ObtenerSesionesPrueba();

        // Act
        var bytes = _service.GenerarReporteAuditoriaExcel(sesiones, "Aula 1 - Últimos 7 días");

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // Archivos XLSX son paquetes ZIP, inician con la firma PK\x03\x04
        bytes[0].Should().Be(0x50); // 'P'
        bytes[1].Should().Be(0x4B); // 'K'
        bytes[2].Should().Be(0x03);
        bytes[3].Should().Be(0x04);
    }

    [Fact]
    public void GenerarReporteAuditoriaExcel_SinDatos_DebeRetornarExcelConPlantillaVacia()
    {
        // Arrange
        var sesiones = new List<SesionAuditoriaDto>();

        // Act
        var bytes = _service.GenerarReporteAuditoriaExcel(sesiones);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(500);
        bytes[0].Should().Be(0x50); // 'P'
        bytes[1].Should().Be(0x4B); // 'K'
    }

    [Fact]
    public void GenerarReporteAuditoriaCsv_ConDatos_DebeIncluirBOMYCabecerasEsperadas()
    {
        // Arrange
        var sesiones = ObtenerSesionesPrueba();

        // Act
        var bytes = _service.GenerarReporteAuditoriaCsv(sesiones);
        var csvString = Encoding.UTF8.GetString(bytes);

        // Assert
        bytes.Should().NotBeNull();
        // Verificar UTF-8 BOM
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        // Verificar encabezados y registros
        csvString.Should().Contain("ID_Sesion;Aula;Terminal;Correo_Estudiante");
        csvString.Should().Contain("LAB1-PC01");
        csvString.Should().Contain("estudiante.test@est.univalle.edu");
        csvString.Should().Contain("LAB1-PC02");
        csvString.Should().Contain("maria.perez@est.univalle.edu");
        csvString.Should().Contain("90");
        csvString.Should().Contain("Manual");
        csvString.Should().Contain("FinPeriodo");
    }

    private List<RegistroEnergiaExportDto> ObtenerIncidentesEnergiaPrueba()
    {
        return new List<RegistroEnergiaExportDto>
        {
            new(
                Id: 1,
                AulaNombre: "Laboratorio A",
                Hostname: "LAB-A-01",
                UltimoEstudianteEmail: "juan.perez@est.univalle.edu",
                UltimoEstudianteNombre: "Juan Perez",
                FechaDeteccion: new DateTime(2026, 9, 18, 22, 0, 0),
                HorasInactivaEncendida: 3.5,
                MotivoInfraccion: "Equipo encendido fuera de horario de laboratorio"
            ),
            new(
                Id: 2,
                AulaNombre: "Laboratorio B",
                Hostname: "LAB-B-05",
                UltimoEstudianteEmail: "ana.gomez@est.univalle.edu",
                UltimoEstudianteNombre: "Ana Gomez",
                FechaDeteccion: new DateTime(2026, 9, 18, 23, 0, 0),
                HorasInactivaEncendida: 4.0,
                MotivoInfraccion: "Equipo encendido fuera de horario de laboratorio"
            )
        };
    }

    [Fact]
    public void GenerarReporteEnergiaExcel_ConDatos_DebeRetornarBytesValidosDeArchivoExcel()
    {
        // Arrange
        var incidentes = ObtenerIncidentesEnergiaPrueba();

        // Act
        var bytes = _service.GenerarReporteEnergiaExcel(incidentes, "Reporte Auditoría Nocturna");

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);
        bytes[0].Should().Be(0x50); // 'P'
        bytes[1].Should().Be(0x4B); // 'K'
        bytes[2].Should().Be(0x03);
        bytes[3].Should().Be(0x04);
    }

    [Fact]
    public void GenerarReporteEnergiaCsv_ConDatos_DebeIncluirBOMYCabecerasEsperadas()
    {
        // Arrange
        var incidentes = ObtenerIncidentesEnergiaPrueba();

        // Act
        var bytes = _service.GenerarReporteEnergiaCsv(incidentes);
        var csvString = Encoding.UTF8.GetString(bytes);

        // Assert
        bytes.Should().NotBeNull();
        // Verificar UTF-8 BOM
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        // Verificar encabezados y datos
        csvString.Should().Contain("ID_Incidente;Aula;Terminal;Ultimo_Estudiante");
        csvString.Should().Contain("juan.perez@est.univalle.edu");
        csvString.Should().Contain("LAB-A-01");
        csvString.Should().Contain("ana.gomez@est.univalle.edu");
        csvString.Should().Contain("LAB-B-05");
        csvString.Should().Contain("3.5");
        csvString.Should().Contain("4");
    }
}

