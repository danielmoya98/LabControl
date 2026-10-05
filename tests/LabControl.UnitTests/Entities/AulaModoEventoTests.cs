using LabControl.Domain.Entities;
using Xunit;

namespace LabControl.UnitTests.Entities;

public class AulaModoEventoTests
{
    [Fact]
    public void IniciarModoEvento_ConDuracion_DebeConfigurarEstadoYFechaFin()
    {
        // Arrange
        var aula = Aula.Create("Lab 101", 25).Value;

        // Act
        aula.IniciarModoEvento("Taller de Robótica", 120);

        // Assert
        Assert.True(aula.ModoEventoActivo);
        Assert.Equal("Taller de Robótica", aula.ModoEventoNombre);
        Assert.NotNull(aula.ModoEventoFinUtc);
        Assert.True(aula.ModoEventoFinUtc.Value > DateTime.UtcNow.AddMinutes(110));
    }

    [Fact]
    public void IniciarModoEvento_SinDuracion_DebeConfigurarEstadoIndefinido()
    {
        // Arrange
        var aula = Aula.Create("Lab 102", 20).Value;

        // Act
        aula.IniciarModoEvento("Jornada de Puertas Abiertas", 0);

        // Assert
        Assert.True(aula.ModoEventoActivo);
        Assert.Equal("Jornada de Puertas Abiertas", aula.ModoEventoNombre);
        Assert.Null(aula.ModoEventoFinUtc);
    }

    [Fact]
    public void FinalizarModoEvento_DebeLimpiarEstadoYCampos()
    {
        // Arrange
        var aula = Aula.Create("Lab 103", 30).Value;
        aula.IniciarModoEvento("Hackathon", 60);

        // Act
        aula.FinalizarModoEvento();

        // Assert
        Assert.False(aula.ModoEventoActivo);
        Assert.Null(aula.ModoEventoFinUtc);
        Assert.Null(aula.ModoEventoNombre);
    }
}
