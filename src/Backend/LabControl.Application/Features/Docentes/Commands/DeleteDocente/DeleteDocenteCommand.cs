using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Docentes.Commands.DeleteDocente;

public record DeleteDocenteCommand(int Id) : IRequest<Result>;

public class DeleteDocenteCommandHandler : IRequestHandler<DeleteDocenteCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteDocenteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteDocenteCommand request, CancellationToken cancellationToken)
    {
        var docente = await _context.Docentes
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (docente == null)
        {
            return Result.Failure(Error.NotFound("Docente.NotFound", "No se encontró el docente especificado."));
        }

        var tieneBloques = await _context.BloquesHorarios
            .AnyAsync(b => b.DocenteId == request.Id, cancellationToken);

        if (tieneBloques)
        {
            docente.Desactivar();
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        _context.Docentes.Remove(docente);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
