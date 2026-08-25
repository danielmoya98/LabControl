using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Aulas.Queries.GetAulas;

public record GetAulasQuery : IRequest<Result<List<AulaDto>>>;

public class GetAulasQueryHandler : IRequestHandler<GetAulasQuery, Result<List<AulaDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAulasQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AulaDto>>> Handle(GetAulasQuery request, CancellationToken cancellationToken)
    {
        var aulas = await _context.Aulas
            .Include(a => a.Computadoras)
            .OrderBy(a => a.Nombre)
            .Select(a => new AulaDto(
                a.Id,
                a.Nombre,
                a.Capacidad,
                a.Pabellon,
                a.Activo,
                a.Computadoras.Count
            ))
            .ToListAsync(cancellationToken);

        return Result<List<AulaDto>>.Success(aulas);
    }
}
