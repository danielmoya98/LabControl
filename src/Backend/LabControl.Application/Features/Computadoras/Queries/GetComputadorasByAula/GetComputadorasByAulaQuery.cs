using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Computadoras.Queries.GetComputadorasByAula;

public record ComputadoraDetalleDto(
    int Id,
    int AulaId,
    string AulaNombre,
    string Hostname,
    string IpActual,
    string MacAddress,
    string EstadoActual,
    DateTime? UltimoHeartbeatUtc,
    string? EmailEstudianteActivo,
    string? CpuModelo = null,
    int? RamTotalGb = null,
    int? DiscoTotalGb = null,
    int? DiscoLibreGb = null,
    string? SistemaOperativo = null,
    double? UptimeHoras = null,
    DateTime? UltimaActualizacionHardwareUtc = null
);

public record GetComputadorasByAulaQuery(int AulaId) : IRequest<Result<List<ComputadoraDetalleDto>>>;

public class GetComputadorasByAulaQueryHandler : IRequestHandler<GetComputadorasByAulaQuery, Result<List<ComputadoraDetalleDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetComputadorasByAulaQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ComputadoraDetalleDto>>> Handle(GetComputadorasByAulaQuery request, CancellationToken cancellationToken)
    {
        var computadoras = await _context.Computadoras
            .Include(c => c.Aula)
            .Include(c => c.SesionesUso)
            .Where(c => c.AulaId == request.AulaId)
            .OrderBy(c => c.Hostname)
            .ToListAsync(cancellationToken);

        var list = computadoras.Select(c =>
        {
            var sesionActiva = c.SesionesUso.FirstOrDefault(s => s.FechaHoraFin == null);
            return new ComputadoraDetalleDto(
                c.Id,
                c.AulaId,
                c.Aula?.Nombre ?? "",
                c.Hostname,
                c.IpActual,
                c.MacAddress,
                c.EstadoActual.ToString(),
                c.UltimoHeartbeatUtc,
                sesionActiva?.EmailEstudiante,
                c.CpuModelo,
                c.RamTotalGb,
                c.DiscoTotalGb,
                c.DiscoLibreGb,
                c.SistemaOperativo,
                c.UptimeHoras,
                c.UltimaActualizacionHardwareUtc
            );
        }).ToList();

        return Result<List<ComputadoraDetalleDto>>.Success(list);
    }
}
