using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Materias.Commands.DeleteMateria;

public record DeleteMateriaCommand(int Id) : IRequest<Result>;

public class DeleteMateriaCommandHandler : IRequestHandler<DeleteMateriaCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteMateriaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteMateriaCommand request, CancellationToken cancellationToken)
    {
        var materia = await _context.Materias
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (materia == null)
        {
            return Result.Failure(Error.NotFound("Materia.NotFound", "No se encontró la materia especificada."));
        }

        // Verificar si está asociada a bloques de horario
        var tieneBloques = await _context.BloquesHorarios
            .AnyAsync(b => b.MateriaId == request.Id, cancellationToken);

        if (tieneBloques)
        {
            // Desactivar suavemente si tiene horarios programados
            materia.Desactivar();
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        _context.Materias.Remove(materia);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
