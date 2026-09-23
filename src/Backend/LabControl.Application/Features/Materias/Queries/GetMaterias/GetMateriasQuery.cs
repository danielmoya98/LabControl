using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Materias.Queries.GetMaterias;

public record MateriaDto(
    int Id,
    string Sigla,
    string Nombre,
    string? Carrera,
    bool Activo
);

public record GetMateriasQuery : IRequest<Result<List<MateriaDto>>>;

public class GetMateriasQueryHandler : IRequestHandler<GetMateriasQuery, Result<List<MateriaDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetMateriasQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<MateriaDto>>> Handle(GetMateriasQuery request, CancellationToken cancellationToken)
    {
        var materias = await _context.Materias
            .AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MateriaDto(
                m.Id,
                m.Sigla,
                m.Nombre,
                m.Carrera,
                m.Activo
            ))
            .ToListAsync(cancellationToken);

        return Result<List<MateriaDto>>.Success(materias);
    }
}
