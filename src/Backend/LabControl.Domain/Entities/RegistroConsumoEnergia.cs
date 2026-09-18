using LabControl.Domain.Common;

namespace LabControl.Domain.Entities;

public class RegistroConsumoEnergia : AggregateRoot
{
    public int ComputadoraId { get; private set; }
    public Computadora Computadora { get; private set; } = default!;
    public int AulaId { get; private set; }
    public Aula Aula { get; private set; } = default!;
    public int? SesionUsoId { get; private set; }
    public SesionUso? SesionUso { get; private set; }
    public string UltimoEstudianteEmail { get; private set; } = default!;
    public string? UltimoEstudianteNombre { get; private set; }
    public DateTime FechaDeteccionUtc { get; private set; }
    public double HorasInactivaEncendida { get; private set; }
    public string MotivoInfraccion { get; private set; } = default!;
    public bool Resuelto { get; private set; }

    private RegistroConsumoEnergia() { } // EF Core

    public static Result<RegistroConsumoEnergia> Create(
        int computadoraId,
        int aulaId,
        string ultimoEstudianteEmail,
        string? ultimoEstudianteNombre,
        double horasInactivaEncendida,
        string motivoInfraccion,
        int? sesionUsoId = null)
    {
        if (computadoraId <= 0)
            return Result<RegistroConsumoEnergia>.Failure(Error.Validation("RegistroConsumoEnergia.ComputadoraRequired", "La computadora es requerida."));

        if (aulaId <= 0)
            return Result<RegistroConsumoEnergia>.Failure(Error.Validation("RegistroConsumoEnergia.AulaRequired", "El aula es requerida."));

        if (string.IsNullOrWhiteSpace(ultimoEstudianteEmail))
            return Result<RegistroConsumoEnergia>.Failure(Error.Validation("RegistroConsumoEnergia.EmailRequired", "El email del último estudiante es requerido."));

        var registro = new RegistroConsumoEnergia
        {
            ComputadoraId = computadoraId,
            AulaId = aulaId,
            SesionUsoId = sesionUsoId,
            UltimoEstudianteEmail = ultimoEstudianteEmail.Trim().ToLowerInvariant(),
            UltimoEstudianteNombre = ultimoEstudianteNombre?.Trim(),
            FechaDeteccionUtc = DateTime.UtcNow,
            HorasInactivaEncendida = Math.Round(horasInactivaEncendida, 1),
            MotivoInfraccion = string.IsNullOrWhiteSpace(motivoInfraccion) ? "Equipo encendido fuera de clase" : motivoInfraccion.Trim(),
            Resuelto = false
        };

        return Result<RegistroConsumoEnergia>.Success(registro);
    }

    public void MarcarResuelto()
    {
        Resuelto = true;
        FechaModificacionUtc = DateTime.UtcNow;
    }
}
