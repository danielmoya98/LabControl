using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Docentes.Queries.GetDocentes;

public record DocenteDto(
    int Id,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string EmailInstitucional,
    string? TelefonoContacto,
    bool Activo
);

public record GetDocentesQuery : IRequest<Result<List<DocenteDto>>>;

public class GetDocentesQueryHandler : IRequestHandler<GetDocentesQuery, Result<List<DocenteDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDocentesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<DocenteDto>>> Handle(GetDocentesQuery request, CancellationToken cancellationToken)
    {
        var docentes = await _context.Docentes
            .AsNoTracking()
            .OrderBy(d => d.Apellidos)
            .ThenBy(d => d.Nombres)
            .Select(d => new DocenteDto(
                d.Id,
                d.Nombres,
                d.Apellidos,
                d.NombreCompleto,
                d.EmailInstitucional,
                d.TelefonoContacto,
                d.Activo
            ))
            .ToListAsync(cancellationToken);

        return Result<List<DocenteDto>>.Success(docentes);
    }
}
