using FluentAssertions;
using LabControl.Domain.ValueObjects;

namespace LabControl.UnitTests.ValueObjects;

public class EmailInstitucionalTests
{
    [Theory]
    [InlineData("estudiante@est.univalle.edu", true, false, "Estudiante")]
    [InlineData("juan.perez123@est.univalle.edu", true, false, "Estudiante")]
    [InlineData("MARIA_GARCIA@EST.UNIVALLE.EDU", true, false, "Estudiante")]
    [InlineData("docente@univalle.edu", false, true, "Docente")]
    [InlineData("carlos.fernandez@univalle.edu", false, true, "Docente")]
    public void Create_ConCorreoInstitucionalValido_DebeRetornarExito(string emailInput, bool esEstudiante, bool esDocente, string tipoEsperado)
    {
        // Act
        var result = EmailInstitucional.Create(emailInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(emailInput.Trim().ToLowerInvariant());
        result.Value.EsEstudiante.Should().Be(esEstudiante);
        result.Value.EsDocente.Should().Be(esDocente);
        result.Value.TipoUsuario.Should().Be(tipoEsperado);
    }

    [Theory]
    [InlineData("estudiante@gmail.com")]
    [InlineData("usuario@hotmail.com")]
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
