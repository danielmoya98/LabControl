using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;

namespace LabControl.Application.Features.Bloques.Queries.GetBloques;

public record BloqueDto(
    int Id,
    int SedeId,
    string Nombre,
    string Codigo,
    bool TienePisos,
    int? TotalPisos,
    string? Descripcion,
    bool Activo,
    int TotalAulas
);

public record GetBloquesQuery : IRequest<Result<List<BloqueDto>>>;

public class GetBloquesQueryHandler : IRequestHandler<GetBloquesQuery, Result<List<BloqueDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetBloquesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BloqueDto>>> Handle(GetBloquesQuery request, CancellationToken cancellationToken)
    {
        var bloques = await _context.Bloques
            .AsNoTracking()
            .OrderBy(b => b.Nombre)
            .Select(b => new BloqueDto(
                b.Id,
                b.SedeId,
                b.Nombre,
                b.Codigo,
                b.TienePisos,
                b.TotalPisos,
                b.Descripcion,
                b.Activo,
                b.Aulas.Count
            ))
            .ToListAsync(cancellationToken);

        return Result<List<BloqueDto>>.Success(bloques);
    }
}
