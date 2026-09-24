using LabControl.Domain.Common;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Domain.Entities;

public class Computadora : AggregateRoot
{
    public int AulaId { get; private set; }
    public Aula Aula { get; private set; } = default!;
    public int NumeroPuesto { get; private set; } = 1;
    public string Hostname { get; private set; } = default!;
    public string IpActual { get; private set; } = default!;
    public string MacAddress { get; private set; } = default!;
    public EstadoComputadora EstadoActual { get; private set; } = EstadoComputadora.Disponible;
    public DateTime? UltimoHeartbeatUtc { get; private set; }

    // Especificaciones de Hardware y Salud
    public string? CpuModelo { get; private set; }
    public int? RamTotalGb { get; private set; }
    public int? DiscoTotalGb { get; private set; }
    public int? DiscoLibreGb { get; private set; }
    public string? SistemaOperativo { get; private set; }
    public double? UptimeHoras { get; private set; }
    public string? DiscosDetalleJson { get; private set; }
    public DateTime? UltimaActualizacionHardwareUtc { get; private set; }

    // Auditoría de Responsabilidad Energética
    public string? UltimoEstudianteEmail { get; private set; }
    public string? UltimoEstudianteNombre { get; private set; }
    public DateTime? FechaUltimoUsoUtc { get; private set; }

    private readonly List<SesionUso> _sesionesUso = [];
    public IReadOnlyCollection<SesionUso> SesionesUso => _sesionesUso.AsReadOnly();

    private Computadora() { } // EF Core

    public static Result<Computadora> Create(int aulaId, string hostname, IpAddress ip, MacAddress mac, int numeroPuesto = 1)
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
            NumeroPuesto = numeroPuesto > 0 ? numeroPuesto : 1,
            Hostname = hostname.Trim().ToUpperInvariant(),
            IpActual = ip.Value,
            MacAddress = mac.Value,
            EstadoActual = EstadoComputadora.Disponible,
            UltimoHeartbeatUtc = DateTime.UtcNow
        };

        return Result<Computadora>.Success(computadora);
    }

    public void AsignarNumeroPuesto(int numeroPuesto)
    {
        if (numeroPuesto > 0)
        {
            NumeroPuesto = numeroPuesto;
            FechaModificacionUtc = DateTime.UtcNow;
        }
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

    public void ActualizarEspecificacionesHardware(
        string? cpuModelo,
        int? ramTotalGb,
        int? discoTotalGb,
        int? discoLibreGb,
        string? sistemaOperativo,
        double? uptimeHoras,
        string? discosDetalleJson = null)
    {
        if (!string.IsNullOrWhiteSpace(cpuModelo)) CpuModelo = cpuModelo.Trim();
        if (ramTotalGb.HasValue && ramTotalGb.Value > 0) RamTotalGb = ramTotalGb.Value;
        if (discoTotalGb.HasValue && discoTotalGb.Value > 0) DiscoTotalGb = discoTotalGb.Value;
        if (discoLibreGb.HasValue && discoLibreGb.Value >= 0) DiscoLibreGb = discoLibreGb.Value;
        if (!string.IsNullOrWhiteSpace(sistemaOperativo)) SistemaOperativo = sistemaOperativo.Trim();
        if (uptimeHoras.HasValue && uptimeHoras.Value >= 0) UptimeHoras = Math.Round(uptimeHoras.Value, 1);
        if (!string.IsNullOrWhiteSpace(discosDetalleJson)) DiscosDetalleJson = discosDetalleJson.Trim();
        UltimaActualizacionHardwareUtc = DateTime.UtcNow;
        FechaModificacionUtc = DateTime.UtcNow;
    }

    public void CambiarEstado(EstadoComputadora nuevoEstado)
    {
        EstadoActual = nuevoEstado;
        FechaModificacionUtc = DateTime.UtcNow;
    }

    public void RegistrarUltimoUsuario(string email, string? nombre = null)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            UltimoEstudianteEmail = email.Trim().ToLowerInvariant();
            UltimoEstudianteNombre = nombre?.Trim();
            FechaUltimoUsoUtc = DateTime.UtcNow;
            FechaModificacionUtc = DateTime.UtcNow;
        }
    }

    public void Update(int aulaId, string hostname, IpAddress ip, MacAddress mac, EstadoComputadora? nuevoEstado = null)
    {
        if (aulaId > 0) AulaId = aulaId;
        if (!string.IsNullOrWhiteSpace(hostname)) Hostname = hostname.Trim().ToUpperInvariant();
        IpActual = ip.Value;
        MacAddress = mac.Value;
        if (nuevoEstado.HasValue) EstadoActual = nuevoEstado.Value;
        FechaModificacionUtc = DateTime.UtcNow;
    }
}
