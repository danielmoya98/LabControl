using LabControl.Domain.Common;

namespace LabControl.Domain.Entities;

public class Docente : AggregateRoot
{
    public string Nombres { get; private set; } = default!;
    public string Apellidos { get; private set; } = default!;
    public string EmailInstitucional { get; private set; } = default!;
    public string? TelefonoContacto { get; private set; }
    public bool Activo { get; private set; } = true;

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();

    private readonly List<BloqueHorario> _bloquesHorarios = [];
    public IReadOnlyCollection<BloqueHorario> BloquesHorarios => _bloquesHorarios.AsReadOnly();

    private Docente() { } // EF Core

    public static Result<Docente> Create(
        string nombres,
        string apellidos,
        string emailInstitucional,
        string? telefonoContacto = null)
    {
        if (string.IsNullOrWhiteSpace(nombres))
            return Result<Docente>.Failure(Error.Validation("Docente.NombresRequired", "Los nombres son obligatorios."));

        if (string.IsNullOrWhiteSpace(apellidos))
            return Result<Docente>.Failure(Error.Validation("Docente.ApellidosRequired", "Los apellidos son obligatorios."));

        if (string.IsNullOrWhiteSpace(emailInstitucional) || !emailInstitucional.Contains('@'))
            return Result<Docente>.Failure(Error.Validation("Docente.EmailInvalid", "Correo institucional inválido."));

        var docente = new Docente
        {
            Nombres = nombres.Trim(),
            Apellidos = apellidos.Trim(),
            EmailInstitucional = emailInstitucional.Trim().ToLowerInvariant(),
            TelefonoContacto = telefonoContacto?.Trim(),
            Activo = true
        };

        return Result<Docente>.Success(docente);
    }

    public void Update(string nombres, string apellidos, string emailInstitucional, string? telefonoContacto)
    {
        Nombres = nombres.Trim();
        Apellidos = apellidos.Trim();
        EmailInstitucional = emailInstitucional.Trim().ToLowerInvariant();
        TelefonoContacto = telefonoContacto?.Trim();
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
