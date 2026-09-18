using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Energia.Queries.GetReporteEnergia;

public record GetReporteEnergiaQuery(
    DateTime? FechaInicio = null,
    DateTime? FechaFin = null,
    int? AulaId = null,
    string? EmailEstudiante = null
) : IRequest<Result<ReporteEnergiaDto>>;

public record ReporteEnergiaDto(
    double TotalHorasDesperdiciadas,
    int TotalIncidentes,
    int TotalEquiposAfectados,
    List<RankingInfractorDto> TopInfractores,
    List<RegistroEnergiaDetalleDto> Incidentes
);

public record RankingInfractorDto(
    string EstudianteEmail,
    string? EstudianteNombre,
    int CantidadIncidentes,
    double TotalHorasDesperdiciadas
);

public record RegistroEnergiaDetalleDto(
    int Id,
    int ComputadoraId,
    string Hostname,
    int AulaId,
    string AulaNombre,
    string UltimoEstudianteEmail,
    string? UltimoEstudianteNombre,
    DateTime FechaDeteccionUtc,
    double HorasInactivaEncendida,
    string MotivoInfraccion,
    bool Resuelto
);

public class GetReporteEnergiaQueryHandler : IRequestHandler<GetReporteEnergiaQuery, Result<ReporteEnergiaDto>>
{
    private readonly IApplicationDbContext _context;

    public GetReporteEnergiaQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ReporteEnergiaDto>> Handle(GetReporteEnergiaQuery request, CancellationToken cancellationToken)
    {
        var query = _context.RegistrosConsumoEnergia
            .Include(r => r.Computadora)
            .Include(r => r.Aula)
            .AsNoTracking()
            .AsQueryable();

        if (request.FechaInicio.HasValue)
        {
            var fInicioUtc = DateTime.SpecifyKind(request.FechaInicio.Value, DateTimeKind.Utc);
            query = query.Where(r => r.FechaDeteccionUtc >= fInicioUtc);
        }

        if (request.FechaFin.HasValue)
        {
            var fFinUtc = DateTime.SpecifyKind(request.FechaFin.Value, DateTimeKind.Utc);
            query = query.Where(r => r.FechaDeteccionUtc <= fFinUtc);
        }

        if (request.AulaId.HasValue && request.AulaId.Value > 0)
        {
            query = query.Where(r => r.AulaId == request.AulaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EmailEstudiante))
        {
            var emailNorm = request.EmailEstudiante.Trim().ToLowerInvariant();
            query = query.Where(r => r.UltimoEstudianteEmail.Contains(emailNorm));
        }

        var incidentesEntities = await query
            .OrderByDescending(r => r.FechaDeteccionUtc)
            .ToListAsync(cancellationToken);

        var totalHoras = Math.Round(incidentesEntities.Sum(r => r.HorasInactivaEncendida), 1);
        var totalIncidentes = incidentesEntities.Count;
        var totalEquipos = incidentesEntities.Select(r => r.ComputadoraId).Distinct().Count();

        var topInfractores = incidentesEntities
            .GroupBy(r => r.UltimoEstudianteEmail)
            .Select(g => new RankingInfractorDto(
                g.Key,
                g.FirstOrDefault()?.UltimoEstudianteNombre,
                g.Count(),
                Math.Round(g.Sum(x => x.HorasInactivaEncendida), 1)
            ))
            .OrderByDescending(x => x.CantidadIncidentes)
            .ThenByDescending(x => x.TotalHorasDesperdiciadas)
            .Take(5)
            .ToList();

        var incidentesDto = incidentesEntities.Select(r => new RegistroEnergiaDetalleDto(
            r.Id,
            r.ComputadoraId,
            r.Computadora?.Hostname ?? $"PC-{r.ComputadoraId}",
            r.AulaId,
            r.Aula?.Nombre ?? $"Aula-{r.AulaId}",
            r.UltimoEstudianteEmail,
            r.UltimoEstudianteNombre,
            r.FechaDeteccionUtc,
            r.HorasInactivaEncendida,
            r.MotivoInfraccion,
            r.Resuelto
        )).ToList();

        return Result<ReporteEnergiaDto>.Success(new ReporteEnergiaDto(
            totalHoras,
            totalIncidentes,
            totalEquipos,
            topInfractores,
            incidentesDto
        ));
    }
}
