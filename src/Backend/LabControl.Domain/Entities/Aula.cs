using LabControl.Domain.Common;

namespace LabControl.Domain.Entities;

public class Aula : AggregateRoot
{
    public string Nombre { get; private set; } = default!;
    public int Capacidad { get; private set; }
    public string? Pabellon { get; private set; }
    public bool Activo { get; private set; } = true;

    private readonly List<Computadora> _computadoras = [];
    public IReadOnlyCollection<Computadora> Computadoras => _computadoras.AsReadOnly();

    private readonly List<BloqueHorario> _bloquesHorarios = [];
    public IReadOnlyCollection<BloqueHorario> BloquesHorarios => _bloquesHorarios.AsReadOnly();

    private Aula() { } // Para EF Core

    public static Result<Aula> Create(string nombre, int capacidad, string? pabellon = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Result<Aula>.Failure(Error.Validation("Aula.NombreRequired", "El nombre del aula es obligatorio."));
        }

        if (capacidad <= 0)
        {
            return Result<Aula>.Failure(Error.Validation("Aula.CapacidadInvalid", "La capacidad del aula debe ser mayor a 0."));
        }

        var aula = new Aula
        {
            Nombre = nombre.Trim(),
            Capacidad = capacidad,
            Pabellon = pabellon?.Trim(),
            Activo = true
        };

        return Result<Aula>.Success(aula);
    }

    public void Update(string nombre, int capacidad, string? pabellon)
    {
        Nombre = nombre.Trim();
        Capacidad = capacidad;
        Pabellon = pabellon?.Trim();
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
