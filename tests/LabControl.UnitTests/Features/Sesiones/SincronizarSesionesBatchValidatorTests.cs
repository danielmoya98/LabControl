using FluentAssertions;
using LabControl.Application.Features.Sesiones.Commands.SincronizarSesionesBatch;
using LabControl.Domain.Enums;
using Xunit;

namespace LabControl.UnitTests.Features.Sesiones;

public class SincronizarSesionesBatchValidatorTests
{
    private readonly SincronizarSesionesBatchCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validador_DebeFallar_CuandoHostnameEstaVacio(string? hostname)
    {
        // Arrange
        var command = new SincronizarSesionesBatchCommand(
            Hostname: hostname!,
            Sesiones: new List<SesionOfflineDto>
            {
                new("estudiante@est.univalle.edu", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, TipoCierreSesion.Manual)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Hostname");
    }

    [Fact]
    public void Validador_DebeFallar_CuandoListaSesionesEstaVacia()
    {
        // Arrange
        var command = new SincronizarSesionesBatchCommand(
            Hostname: "LAB1-PC01",
            Sesiones: new List<SesionOfflineDto>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Sesiones");
    }

    [Fact]
    public void Validador_DebeTenerExito_ConComandoValido()
    {
        // Arrange
        var command = new SincronizarSesionesBatchCommand(
            Hostname: "LAB1-PC01",
            Sesiones: new List<SesionOfflineDto>
            {
                new("juan.perez@est.univalle.edu", DateTime.UtcNow.AddMinutes(-45), DateTime.UtcNow, TipoCierreSesion.Manual),
                new("ana.gomez@est.univalle.edu", DateTime.UtcNow.AddMinutes(-90), DateTime.UtcNow.AddMinutes(-45), TipoCierreSesion.FinPeriodo)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
