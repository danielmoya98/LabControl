using LabControl.Domain.Common;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Domain.Entities;

public class Computadora : AggregateRoot
{
    public int AulaId { get; private set; }
    public Aula Aula { get; private set; } = default!;
    public string Hostname { get; private set; } = default!;
    public string IpActual { get; private set; } = default!;
    public string MacAddress { get; private set; } = default!;
    public EstadoComputadora EstadoActual { get; private set; } = EstadoComputadora.Disponible;
    public DateTime? UltimoHeartbeatUtc { get; private set; }

    private readonly List<SesionUso> _sesionesUso = [];
    public IReadOnlyCollection<SesionUso> SesionesUso => _sesionesUso.AsReadOnly();

    private Computadora() { } // EF Core

    public static Result<Computadora> Create(int aulaId, string hostname, IpAddress ip, MacAddress mac)
    {
        if (aulaId <= 0)
        {
            return Result<Computadora>.Failure(Error.Validation("Computadora.AulaRequired", "Debe asociar la PC a un aula válida."));
        }

        if (string.IsNullOrWhiteSpace(hostname))
        {
            return Result<Computadora>.Failure(Error.Validation("Computadora.HostnameRequired", "El Hostname es obligatorio."));
        }

        var computadora = new Computadora
        {
            AulaId = aulaId,
            Hostname = hostname.Trim().ToUpperInvariant(),
            IpActual = ip.Value,
            MacAddress = mac.Value,
            EstadoActual = EstadoComputadora.Disponible,
            UltimoHeartbeatUtc = DateTime.UtcNow
        };

        return Result<Computadora>.Success(computadora);
    }

    public void ActualizarHeartbeat(IpAddress ip)
    {
        IpActual = ip.Value;
        UltimoHeartbeatUtc = DateTime.UtcNow;
    }

    public void ActualizarUbicacionRed(IpAddress ip, MacAddress mac)
    {
        IpActual = ip.Value;
        MacAddress = mac.Value;
        UltimoHeartbeatUtc = DateTime.UtcNow;
        FechaModificacionUtc = DateTime.UtcNow;
    }

    public void AsignarAula(int aulaId)
    {
        if (aulaId > 0)
        {
            AulaId = aulaId;
            FechaModificacionUtc = DateTime.UtcNow;
        }
    }

    public void CambiarEstado(EstadoComputadora nuevoEstado)
    {
        EstadoActual = nuevoEstado;
        FechaModificacionUtc = DateTime.UtcNow;
    }
}
