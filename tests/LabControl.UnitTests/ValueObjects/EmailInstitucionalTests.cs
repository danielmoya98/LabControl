using FluentAssertions;
using LabControl.Domain.ValueObjects;

namespace LabControl.UnitTests.ValueObjects;

public class EmailInstitucionalTests
{
    [Theory]
    [InlineData("estudiante@est.univalle.edu")]
    [InlineData("juan.perez123@est.univalle.edu")]
    [InlineData("MARIA_GARCIA@EST.UNIVALLE.EDU")]
    public void Create_ConCorreoInstitucionalValido_DebeRetornarExito(string emailInput)
    {
        // Act
        var result = EmailInstitucional.Create(emailInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(emailInput.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("estudiante@gmail.com")]
    [InlineData("estudiante@univalle.edu")]
    [InlineData("invalido@est.otrauniversidad.edu")]
    [InlineData("")]
    [InlineData("    ")]
    public void Create_ConCorreoInvalido_DebeRetornarFalla(string emailInput)
    {
        // Act
        var result = EmailInstitucional.Create(emailInput);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Email.");
    }
}
