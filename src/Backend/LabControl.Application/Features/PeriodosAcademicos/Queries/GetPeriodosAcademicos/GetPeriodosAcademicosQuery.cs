using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.PeriodosAcademicos.Queries.GetPeriodosAcademicos;

public record PeriodoAcademicoDto(
    int Id,
    string Nombre,
    DateTime FechaInicio,
    DateTime FechaFin,
    bool EsActual,
    bool Activo,
    int CantidadBloquesHorarios
);

public record GetPeriodosAcademicosQuery(bool SoloActivos = false) : IRequest<Result<List<PeriodoAcademicoDto>>>;

public class GetPeriodosAcademicosQueryHandler : IRequestHandler<GetPeriodosAcademicosQuery, Result<List<PeriodoAcademicoDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPeriodosAcademicosQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PeriodoAcademicoDto>>> Handle(GetPeriodosAcademicosQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PeriodosAcademicos.AsNoTracking();

        if (request.SoloActivos)
        {
            query = query.Where(p => p.Activo);
        }

        var periodos = await query
            .OrderByDescending(p => p.EsActual)
            .ThenByDescending(p => p.FechaInicio)
            .Select(p => new PeriodoAcademicoDto(
                p.Id,
                p.Nombre,
                p.FechaInicio,
                p.FechaFin,
                p.EsActual,
                p.Activo,
                p.BloquesHorarios.Count
            ))
            .ToListAsync(cancellationToken);

        return Result<List<PeriodoAcademicoDto>>.Success(periodos);
    }
}
