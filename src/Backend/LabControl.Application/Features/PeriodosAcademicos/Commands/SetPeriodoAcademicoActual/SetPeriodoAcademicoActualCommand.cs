using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.PeriodosAcademicos.Commands.SetPeriodoAcademicoActual;

public record SetPeriodoAcademicoActualCommand(int Id) : IRequest<Result<bool>>;

public class SetPeriodoAcademicoActualCommandHandler : IRequestHandler<SetPeriodoAcademicoActualCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public SetPeriodoAcademicoActualCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(SetPeriodoAcademicoActualCommand request, CancellationToken cancellationToken)
    {
        var targetPeriodo = await _context.PeriodosAcademicos
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (targetPeriodo == null)
        {
            return Result<bool>.Failure(
                Error.NotFound("PeriodoAcademico.NotFound", $"No se encontró el período académico con ID {request.Id}."));
        }

        var todos = await _context.PeriodosAcademicos.ToListAsync(cancellationToken);
        foreach (var p in todos)
        {
            if (p.Id == request.Id)
            {
                p.MarcarComoActual();
            }
            else
            {
                p.DesmarcarActual();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
