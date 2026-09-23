using LabControl.Domain.Common;

namespace LabControl.Domain.Entities;

public class Sede : AggregateRoot
{
    public string Nombre { get; private set; } = default!;
    public string Codigo { get; private set; } = default!;
    public string Ciudad { get; private set; } = default!;
    public string? Direccion { get; private set; }
    public string? SegmentoRed { get; private set; }
    public bool OnboardingCompletado { get; private set; }
    public DateTime FechaRegistroUtc { get; private set; }

    private readonly List<Bloque> _bloques = [];
    public IReadOnlyCollection<Bloque> Bloques => _bloques.AsReadOnly();

    private Sede() { } // EF Core

    public static Result<Sede> Create(
        string nombre,
        string codigo,
        string ciudad,
        string? direccion = null,
        bool onboardingCompletado = false,
        string? segmentoRed = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result<Sede>.Failure(Error.Validation("Sede.NombreRequired", "El nombre de la sede es obligatorio."));

        if (string.IsNullOrWhiteSpace(codigo))
            return Result<Sede>.Failure(Error.Validation("Sede.CodigoRequired", "El código de la sede es obligatorio."));

        if (string.IsNullOrWhiteSpace(ciudad))
            return Result<Sede>.Failure(Error.Validation("Sede.CiudadRequired", "La ciudad de la sede es obligatoria."));

        var sede = new Sede
        {
            Nombre = nombre.Trim(),
            Codigo = codigo.Trim().ToUpperInvariant(),
            Ciudad = ciudad.Trim(),
            Direccion = direccion?.Trim(),
            SegmentoRed = segmentoRed?.Trim(),
            OnboardingCompletado = onboardingCompletado,
            FechaRegistroUtc = DateTime.UtcNow
        };

        return Result<Sede>.Success(sede);
    }

    public void Update(string nombre, string codigo, string ciudad, string? direccion, string? segmentoRed = null)
    {
        Nombre = nombre.Trim();
        Codigo = codigo.Trim().ToUpperInvariant();
        Ciudad = ciudad.Trim();
        Direccion = direccion?.Trim();
        if (segmentoRed != null)
        {
            SegmentoRed = string.IsNullOrWhiteSpace(segmentoRed) ? null : segmentoRed.Trim();
        }
        FechaModificacionUtc = DateTime.UtcNow;
    }

    public void UpdateSegmentoRed(string? segmentoRed)
    {
        SegmentoRed = string.IsNullOrWhiteSpace(segmentoRed) ? null : segmentoRed.Trim();
        FechaModificacionUtc = DateTime.UtcNow;
    }

    public bool PerteneceRed(string ip)
    {
        if (string.IsNullOrWhiteSpace(SegmentoRed) || string.IsNullOrWhiteSpace(ip))
            return false;
        return ip.Trim().StartsWith(SegmentoRed.Trim());
    }

    public void MarcarOnboardingCompletado()
    {
        OnboardingCompletado = true;
        FechaModificacionUtc = DateTime.UtcNow;
    }
}
