using FluentAssertions;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;
using Xunit;

namespace LabControl.UnitTests.Entities;

public class BloqueHorarioTests
{
    [Fact]
    public void Create_ConDatosValidos_DebeCrearBloqueExitosamente()
    {
        // Arrange & Act
        var result = BloqueHorario.Create(
            aulaId: 1,
            diaSemana: DiaSemana.Lunes,
            horaInicio: new TimeSpan(7, 30, 0),
            horaFin: new TimeSpan(9, 0, 0),
            esRecreo: false,
            descripcion: "Sistemas Operativos"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AulaId.Should().Be(1);
        result.Value.DiaSemana.Should().Be(DiaSemana.Lunes);
        result.Value.HoraInicio.Should().Be(new TimeSpan(7, 30, 0));
        result.Value.HoraFin.Should().Be(new TimeSpan(9, 0, 0));
        result.Value.EsRecreo.Should().BeFalse();
        result.Value.Descripcion.Should().Be("Sistemas Operativos");
    }

    [Fact]
    public void Create_ConAulaInvalida_DebeRetornarFallo()
    {
        // Act
        var result = BloqueHorario.Create(
            aulaId: 0,
            diaSemana: DiaSemana.Martes,
            horaInicio: new TimeSpan(9, 0, 0),
            horaFin: new TimeSpan(10, 30, 0)
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BloqueHorario.AulaRequired");
    }

    [Fact]
    public void Create_ConHoraFinMenorOIgualAHoraInicio_DebeRetornarFallo()
    {
        // Act
        var result = BloqueHorario.Create(
            aulaId: 1,
            diaSemana: DiaSemana.Miercoles,
            horaInicio: new TimeSpan(10, 0, 0),
            horaFin: new TimeSpan(9, 0, 0) // Menor
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BloqueHorario.InvalidTimeRange");
    }

    [Fact]
    public void Create_ComoRecreo_DebeEstablecerBanderaEsRecreoEnTrue()
    {
        // Act
        var result = BloqueHorario.Create(
            aulaId: 2,
            diaSemana: DiaSemana.Jueves,
            horaInicio: new TimeSpan(10, 30, 0),
            horaFin: new TimeSpan(11, 0, 0),
            esRecreo: true,
            descripcion: "Receso de Mañana"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EsRecreo.Should().BeTrue();
    }

    [Fact]
    public void Update_ConDatosValidos_DebeActualizarPropiedades()
    {
        // Arrange
        var bloque = BloqueHorario.Create(
            aulaId: 1,
            diaSemana: DiaSemana.Viernes,
            horaInicio: new TimeSpan(14, 0, 0),
            horaFin: new TimeSpan(15, 30, 0),
            esRecreo: false,
            descripcion: "Redes I"
        ).Value;

        // Act
        var updateResult = bloque.Update(
            aulaId: 1,
            diaSemana: DiaSemana.Viernes,
            horaInicio: new TimeSpan(14, 30, 0),
            horaFin: new TimeSpan(16, 0, 0),
            esRecreo: false,
            descripcion: "Redes I - Avanzado"
        );

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        bloque.HoraInicio.Should().Be(new TimeSpan(14, 30, 0));
        bloque.HoraFin.Should().Be(new TimeSpan(16, 0, 0));
        bloque.Descripcion.Should().Be("Redes I - Avanzado");
        bloque.FechaModificacionUtc.Should().NotBeNull();
    }
}
