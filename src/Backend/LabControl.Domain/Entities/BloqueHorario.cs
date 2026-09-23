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
    public bool EsUsoLibre { get; private set; }
    public string? Descripcion { get; private set; }

    // Responsabilidad académica
    public int? MateriaId { get; private set; }
    public Materia? Materia { get; private set; }
    public int? DocenteId { get; private set; }
    public Docente? Docente { get; private set; }
    public string? GrupoParalelo { get; private set; }

    // Período Académico / Semestre
    public int? PeriodoAcademicoId { get; private set; }
    public PeriodoAcademico? PeriodoAcademico { get; private set; }

    // Campos manuales opcionales (sin requerir pre-registro en catálogo)
    public string? DocenteNombreManual { get; private set; }
    public string? DocenteEmailManual { get; private set; }
    public string? MateriaNombreManual { get; private set; }

    // Propiedades calculadas efectivas
    public string NombreDocenteEfectivo => Docente?.NombreCompleto ?? (!string.IsNullOrWhiteSpace(DocenteNombreManual) ? DocenteNombreManual : "Sin Docente Asignado");
    public string EmailDocenteEfectivo => Docente?.EmailInstitucional ?? DocenteEmailManual ?? "";
    public string NombreMateriaEfectivo => Materia?.Nombre ?? (!string.IsNullOrWhiteSpace(MateriaNombreManual) ? MateriaNombreManual : (EsRecreo ? "Receso / Mantenimiento" : "Uso Libre"));

    private BloqueHorario() { }

    public static Result<BloqueHorario> Create(
        int aulaId,
        DiaSemana diaSemana,
        TimeSpan horaInicio,
        TimeSpan horaFin,
        bool esRecreo = false,
        string? descripcion = null,
        int? materiaId = null,
        int? docenteId = null,
        string? grupoParalelo = null,
        bool esUsoLibre = false,
        int? periodoAcademicoId = null,
        string? docenteNombreManual = null,
        string? docenteEmailManual = null,
        string? materiaNombreManual = null)
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
            EsUsoLibre = esUsoLibre,
            Descripcion = descripcion?.Trim(),
            MateriaId = materiaId,
            DocenteId = docenteId,
            GrupoParalelo = grupoParalelo?.Trim(),
            PeriodoAcademicoId = periodoAcademicoId,
            DocenteNombreManual = docenteNombreManual?.Trim(),
            DocenteEmailManual = docenteEmailManual?.Trim()?.ToLowerInvariant(),
            MateriaNombreManual = materiaNombreManual?.Trim()
        };

        return Result<BloqueHorario>.Success(bloque);
    }

    public Result Update(
        int aulaId,
        DiaSemana diaSemana,
        TimeSpan horaInicio,
        TimeSpan horaFin,
        bool esRecreo,
        string? descripcion,
        int? materiaId = null,
        int? docenteId = null,
        string? grupoParalelo = null,
        bool esUsoLibre = false,
        int? periodoAcademicoId = null,
        string? docenteNombreManual = null,
        string? docenteEmailManual = null,
        string? materiaNombreManual = null)
    {
        if (aulaId <= 0)
        {
            return Result.Failure(Error.Validation("BloqueHorario.AulaRequired", "Se requiere una aula válida."));
        }

        if (horaFin <= horaInicio)
        {
            return Result.Failure(Error.Validation("BloqueHorario.InvalidTimeRange", "La hora de fin debe ser posterior a la hora de inicio."));
        }

        AulaId = aulaId;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
        EsRecreo = esRecreo;
        EsUsoLibre = esUsoLibre;
        Descripcion = descripcion?.Trim();
        MateriaId = materiaId;
        DocenteId = docenteId;
        GrupoParalelo = grupoParalelo?.Trim();
        if (periodoAcademicoId.HasValue)
        {
            PeriodoAcademicoId = periodoAcademicoId.Value;
        }
        DocenteNombreManual = docenteNombreManual?.Trim();
        DocenteEmailManual = docenteEmailManual?.Trim()?.ToLowerInvariant();
        MateriaNombreManual = materiaNombreManual?.Trim();
        FechaModificacionUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public void AsignarPeriodo(int periodoAcademicoId)
    {
        PeriodoAcademicoId = periodoAcademicoId;
        FechaModificacionUtc = DateTime.UtcNow;
    }
}
