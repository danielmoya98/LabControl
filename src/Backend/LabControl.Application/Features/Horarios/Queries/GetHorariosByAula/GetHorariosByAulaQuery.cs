using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Horarios.Queries.GetHorariosByAula;

public record GetHorariosByAulaQuery(int AulaId) : IRequest<Result<List<BloqueHorarioDto>>>;

public class GetHorariosByAulaQueryHandler : IRequestHandler<GetHorariosByAulaQuery, Result<List<BloqueHorarioDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetHorariosByAulaQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BloqueHorarioDto>>> Handle(GetHorariosByAulaQuery request, CancellationToken cancellationToken)
    {
        var bloques = await _context.BloquesHorarios
            .Where(b => b.AulaId == request.AulaId)
            .OrderBy(b => b.DiaSemana)
            .ThenBy(b => b.HoraInicio)
            .Select(b => new BloqueHorarioDto(
                b.Id,
                b.AulaId,
                b.DiaSemana,
                b.HoraInicio,
                b.HoraFin,
                b.EsRecreo,
                b.Descripcion
            ))
            .ToListAsync(cancellationToken);

        return Result<List<BloqueHorarioDto>>.Success(bloques);
    }
}
