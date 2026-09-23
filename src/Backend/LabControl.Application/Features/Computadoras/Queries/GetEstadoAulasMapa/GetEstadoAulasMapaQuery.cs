using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Computadoras.Queries.GetEstadoAulasMapa;

public record GetEstadoAulasMapaQuery : IRequest<Result<List<AulaEstadoDto>>>;

public record AulaEstadoDto(
    int AulaId,
    string Nombre,
    int Capacidad,
    string? Pabellon,
    List<ComputadoraTarjetaDto> Computadoras
);

public record ComputadoraTarjetaDto(
    int ComputadoraId,
    string Hostname,
    string IpActual,
    string MacAddress,
    EstadoComputadora EstadoActual,
    DateTime? UltimoHeartbeat,
    string? EmailEstudianteActual,
    DateTime? HoraInicioSesion,
    string? CpuModelo = null,
    int? RamTotalGb = null,
    int? DiscoTotalGb = null,
    int? DiscoLibreGb = null,
    string? SistemaOperativo = null,
    double? UptimeHoras = null
)
{
    public string? MiniaturaBase64 { get; set; }
};

public class GetEstadoAulasMapaQueryHandler : IRequestHandler<GetEstadoAulasMapaQuery, Result<List<AulaEstadoDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEstadoAulasMapaQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AulaEstadoDto>>> Handle(GetEstadoAulasMapaQuery request, CancellationToken cancellationToken)
    {
        var aulas = await _context.Aulas
            .Include(a => a.Computadoras)
            .Where(a => a.Activo)
            .OrderBy(a => a.Nombre)
            .ToListAsync(cancellationToken);

        var computadorasIds = aulas.SelectMany(a => a.Computadoras).Select(c => c.Id).ToList();

        var sesionesActivas = await _context.SesionesUso
            .Where(s => computadorasIds.Contains(s.ComputadoraId) && s.FechaHoraFin == null)
            .ToDictionaryAsync(s => s.ComputadoraId, cancellationToken);

        var resultado = aulas.Select(aula => new AulaEstadoDto(
            aula.Id,
            aula.Nombre,
            aula.Capacidad,
            aula.Pabellon,
            aula.Computadoras.Select(pc =>
            {
                sesionesActivas.TryGetValue(pc.Id, out var sesion);
                return new ComputadoraTarjetaDto(
                    pc.Id,
                    pc.Hostname,
                    pc.IpActual,
                    pc.MacAddress,
                    pc.EstadoActual,
                    pc.UltimoHeartbeatUtc,
                    sesion?.EmailEstudiante,
                    sesion?.FechaHoraInicio,
                    pc.CpuModelo,
                    pc.RamTotalGb,
                    pc.DiscoTotalGb,
                    pc.DiscoLibreGb,
                    pc.SistemaOperativo,
                    pc.UptimeHoras
                );
            }).ToList()
        )).ToList();

        return Result<List<AulaEstadoDto>>.Success(resultado);
    }
}
