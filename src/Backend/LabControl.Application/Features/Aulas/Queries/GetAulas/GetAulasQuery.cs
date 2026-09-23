using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Aulas.Queries.GetAulas;

public record GetAulasQuery(int? BloqueId = null) : IRequest<Result<List<AulaDto>>>;

public class GetAulasQueryHandler : IRequestHandler<GetAulasQuery, Result<List<AulaDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAulasQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AulaDto>>> Handle(GetAulasQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Aulas
            .AsNoTracking()
            .Include(a => a.Computadoras)
            .Include(a => a.Bloque)
            .AsQueryable();

        if (request.BloqueId.HasValue)
        {
            query = query.Where(a => a.BloqueId == request.BloqueId.Value);
        }

        var aulas = await query
            .OrderBy(a => a.Nombre)
            .Select(a => new AulaDto(
                a.Id,
                a.Nombre,
                a.Capacidad,
                a.Pabellon,
                a.Activo,
                a.Computadoras.Count,
                a.MinutosInactividadMaximo,
                a.AccionInactividad,
                a.BloqueId,
                a.Bloque != null ? a.Bloque.Nombre : null,
                a.Piso
            ))
            .ToListAsync(cancellationToken);

        return Result<List<AulaDto>>.Success(aulas);
    }
}
