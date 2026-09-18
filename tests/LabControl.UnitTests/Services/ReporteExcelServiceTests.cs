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
}
