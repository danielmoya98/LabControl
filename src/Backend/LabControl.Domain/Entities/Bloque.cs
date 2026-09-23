using LabControl.Domain.Common;

namespace LabControl.Domain.Entities;

public class Bloque : AggregateRoot
{
    public int SedeId { get; private set; }
    public Sede Sede { get; private set; } = default!;
    public string Nombre { get; private set; } = default!;
    public string Codigo { get; private set; } = default!;
    public bool TienePisos { get; private set; }
    public int? TotalPisos { get; private set; }
    public string? Descripcion { get; private set; }
    public bool Activo { get; private set; } = true;

    private readonly List<Aula> _aulas = [];
    public IReadOnlyCollection<Aula> Aulas => _aulas.AsReadOnly();

    private Bloque() { } // EF Core

    public static Result<Bloque> Create(
        int sedeId,
        string nombre,
        string codigo,
        bool tienePisos,
        int? totalPisos = null,
        string? descripcion = null)
    {
        if (sedeId <= 0)
            return Result<Bloque>.Failure(Error.Validation("Bloque.SedeRequired", "Debe asociar el bloque a una sede válida."));

        if (string.IsNullOrWhiteSpace(nombre))
            return Result<Bloque>.Failure(Error.Validation("Bloque.NombreRequired", "El nombre del bloque es obligatorio."));

        if (string.IsNullOrWhiteSpace(codigo))
            return Result<Bloque>.Failure(Error.Validation("Bloque.CodigoRequired", "El código del bloque es obligatorio."));

        if (tienePisos && totalPisos.HasValue && totalPisos.Value < 1)
            return Result<Bloque>.Failure(Error.Validation("Bloque.TotalPisosInvalid", "El total de pisos debe ser al menos 1."));

        var bloque = new Bloque
        {
            SedeId = sedeId,
            Nombre = nombre.Trim(),
            Codigo = codigo.Trim().ToUpperInvariant(),
            TienePisos = tienePisos,
            TotalPisos = tienePisos ? totalPisos : null,
            Descripcion = descripcion?.Trim(),
            Activo = true
        };

        return Result<Bloque>.Success(bloque);
    }

    public void Update(
        string nombre,
        string codigo,
        bool tienePisos,
        int? totalPisos,
        string? descripcion)
    {
        Nombre = nombre.Trim();
        Codigo = codigo.Trim().ToUpperInvariant();
        TienePisos = tienePisos;
        TotalPisos = tienePisos ? totalPisos : null;
        Descripcion = descripcion?.Trim();
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
