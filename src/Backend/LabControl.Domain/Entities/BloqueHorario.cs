using LabControl.Domain.Common;
using LabControl.Domain.Enums;

namespace LabControl.Domain.Entities;

public class BloqueHorario : BaseEntity
{
    public int AulaId { get; private set; }
    public Aula Aula { get; private set; } = default!;
    public DiaSemana DiaSemana { get; private set; }
    public TimeSpan HoraInicio { get; private set; }
    public TimeSpan HoraFin { get; private set; }
    public bool EsRecreo { get; private set; }
    public string? Descripcion { get; private set; }

    private BloqueHorario() { }

    public static Result<BloqueHorario> Create(
        int aulaId,
        DiaSemana diaSemana,
        TimeSpan horaInicio,
        TimeSpan horaFin,
        bool esRecreo = false,
        string? descripcion = null)
    {
        if (aulaId <= 0)
        {
            return Result<BloqueHorario>.Failure(Error.Validation("BloqueHorario.AulaRequired", "Se requiere una aula válida."));
        }

        if (horaFin <= horaInicio)
        {
            return Result<BloqueHorario>.Failure(Error.Validation("BloqueHorario.InvalidTimeRange", "La hora de fin debe ser posterior a la hora de inicio."));
        }

        var bloque = new BloqueHorario
        {
            AulaId = aulaId,
            DiaSemana = diaSemana,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            EsRecreo = esRecreo,
            Descripcion = descripcion?.Trim()
        };

        return Result<BloqueHorario>.Success(bloque);
    }
}
