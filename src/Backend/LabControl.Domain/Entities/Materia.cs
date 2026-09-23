using LabControl.Domain.Common;

namespace LabControl.Domain.Entities;

public class Materia : AggregateRoot
{
    public string Sigla { get; private set; } = default!;
    public string Nombre { get; private set; } = default!;
    public string? Carrera { get; private set; }
    public bool Activo { get; private set; } = true;

    private readonly List<BloqueHorario> _bloquesHorarios = [];
    public IReadOnlyCollection<BloqueHorario> BloquesHorarios => _bloquesHorarios.AsReadOnly();

    private Materia() { } // EF Core

    public static Result<Materia> Create(
        string sigla,
        string nombre,
        string? carrera = null)
    {
        if (string.IsNullOrWhiteSpace(sigla))
            return Result<Materia>.Failure(Error.Validation("Materia.SiglaRequired", "La sigla de la materia es obligatoria."));

        if (string.IsNullOrWhiteSpace(nombre))
            return Result<Materia>.Failure(Error.Validation("Materia.NombreRequired", "El nombre de la materia es obligatorio."));

        var materia = new Materia
        {
            Sigla = sigla.Trim().ToUpperInvariant(),
            Nombre = nombre.Trim(),
            Carrera = carrera?.Trim(),
            Activo = true
        };

        return Result<Materia>.Success(materia);
    }

    public void Update(string sigla, string nombre, string? carrera)
    {
        Sigla = sigla.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Carrera = carrera?.Trim();
        FechaModificacionUtc = DateTime.UtcNow;
    }

    public void Desactivar()
    {
        Activo = false;
        FechaModificacionUtc = DateTime.UtcNow;
    }

    public void Activar()
    {
        Activo = true;
        FechaModificacionUtc = DateTime.UtcNow;
    }
}
