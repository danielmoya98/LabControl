using LabControl.Domain.Common;

namespace LabControl.Domain.Entities;

public class PeriodoAcademico : AggregateRoot
{
    public string Nombre { get; private set; } = default!;
    public DateTime FechaInicio { get; private set; }
    public DateTime FechaFin { get; private set; }
    public bool EsActual { get; private set; }
    public bool Activo { get; private set; } = true;
    public DateTime FechaRegistroUtc { get; private set; }

    private readonly List<BloqueHorario> _bloquesHorarios = [];
    public IReadOnlyCollection<BloqueHorario> BloquesHorarios => _bloquesHorarios.AsReadOnly();

    private PeriodoAcademico() { } // EF Core

    public static Result<PeriodoAcademico> Create(
        string nombre,
        DateTime fechaInicio,
        DateTime fechaFin,
        bool esActual = false)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Result<PeriodoAcademico>.Failure(
                Error.Validation("PeriodoAcademico.NombreRequired", "El nombre del período académico es obligatorio (ej. 'II-2026')."));
        }

        if (fechaFin <= fechaInicio)
        {
            return Result<PeriodoAcademico>.Failure(
                Error.Validation("PeriodoAcademico.InvalidDates", "La fecha de fin debe ser posterior a la fecha de inicio."));
        }

        var periodo = new PeriodoAcademico
        {
            Nombre = nombre.Trim().ToUpperInvariant(),
            FechaInicio = DateTime.SpecifyKind(fechaInicio.Date, DateTimeKind.Unspecified),
            FechaFin = DateTime.SpecifyKind(fechaFin.Date, DateTimeKind.Unspecified),
            EsActual = esActual,
            Activo = true,
            FechaRegistroUtc = DateTime.UtcNow
        };

        return Result<PeriodoAcademico>.Success(periodo);
    }

    public void MarcarComoActual()
    {
        EsActual = true;
        Activo = true;
    }

    public void DesmarcarActual()
    {
        EsActual = false;
    }

    public Result Actualizar(string nombre, DateTime fechaInicio, DateTime fechaFin, bool activo)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Result.Failure(
                Error.Validation("PeriodoAcademico.NombreRequired", "El nombre del período académico es obligatorio."));
        }

        if (fechaFin <= fechaInicio)
        {
            return Result.Failure(
                Error.Validation("PeriodoAcademico.InvalidDates", "La fecha de fin debe ser posterior a la fecha de inicio."));
        }

        Nombre = nombre.Trim().ToUpperInvariant();
        FechaInicio = fechaInicio.Date;
        FechaFin = fechaFin.Date;
        Activo = activo;

        return Result.Success();
    }
}
