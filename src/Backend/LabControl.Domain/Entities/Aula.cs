using LabControl.Domain.Common;
using LabControl.Domain.Enums;

namespace LabControl.Domain.Entities;

public class Aula : AggregateRoot
{
    public string Nombre { get; private set; } = default!;
    public int Capacidad { get; private set; }
    public string? Pabellon { get; private set; }
    public int? BloqueId { get; private set; }
    public Bloque? Bloque { get; private set; }
    public string? Piso { get; private set; }
    public bool Activo { get; private set; } = true;
    public int MinutosInactividadMaximo { get; private set; } = 15;
    public TipoAccionInactividad AccionInactividad { get; private set; } = TipoAccionInactividad.ApagarEquipo;

    private readonly List<Computadora> _computadoras = [];
    public IReadOnlyCollection<Computadora> Computadoras => _computadoras.AsReadOnly();

    private readonly List<BloqueHorario> _bloquesHorarios = [];
    public IReadOnlyCollection<BloqueHorario> BloquesHorarios => _bloquesHorarios.AsReadOnly();

    private Aula() { } // Para EF Core

    public static Result<Aula> Create(
        string nombre, 
        int capacidad, 
        string? pabellon = null, 
        int minutosInactividad = 15, 
        TipoAccionInactividad accionInactividad = TipoAccionInactividad.ApagarEquipo,
        int? bloqueId = null,
        string? piso = null)
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
            BloqueId = bloqueId,
            Piso = piso?.Trim(),
            MinutosInactividadMaximo = minutosInactividad > 0 ? minutosInactividad : 15,
            AccionInactividad = accionInactividad,
            Activo = true
        };

        return Result<Aula>.Success(aula);
    }

    public void Update(
        string nombre, 
        int capacidad, 
        string? pabellon, 
        int minutosInactividad = 15, 
        TipoAccionInactividad accionInactividad = TipoAccionInactividad.ApagarEquipo,
        int? bloqueId = null,
        string? piso = null)
    {
        Nombre = nombre.Trim();
        Capacidad = capacidad;
        Pabellon = pabellon?.Trim();
        BloqueId = bloqueId ?? BloqueId;
        Piso = piso?.Trim() ?? Piso;
        MinutosInactividadMaximo = minutosInactividad > 0 ? minutosInactividad : 15;
        AccionInactividad = accionInactividad;
        FechaModificacionUtc = DateTime.UtcNow;
    }

    public void AsignarBloqueYPiso(int bloqueId, string? piso)
    {
        BloqueId = bloqueId;
        Piso = piso?.Trim();
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
