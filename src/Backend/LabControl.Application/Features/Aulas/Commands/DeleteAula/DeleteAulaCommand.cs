using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Aulas.Commands.DeleteAula;

public record DeleteAulaCommand(
    int Id,
    bool SoftDelete = true
) : IRequest<Result<bool>>;

public class DeleteAulaCommandValidator : AbstractValidator<DeleteAulaCommand>
{
    public DeleteAulaCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador del aula es inválido.");
    }
}

public class DeleteAulaCommandHandler : IRequestHandler<DeleteAulaCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteAulaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteAulaCommand request, CancellationToken cancellationToken)
    {
        var aula = await _context.Aulas
            .Include(a => a.Computadoras)
            .Include(a => a.BloquesHorarios)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (aula == null)
        {
            return Result<bool>.Failure(Error.NotFound("Aula.NotFound", $"No se encontró el aula con ID {request.Id}."));
        }

        if (request.SoftDelete)
        {
            aula.Desactivar();
            await _context.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        // Si es Hard Delete, validar que no tenga computadoras asociadas para evitar violar integridad referencial
        if (aula.Computadoras.Any())
        {
            return Result<bool>.Failure(Error.Validation("Aula.HasComputadoras",
                $"No se puede eliminar físicamente el aula '{aula.Nombre}' porque tiene {aula.Computadoras.Count} computadora(s) asignada(s). Reasigne o elimine las computadoras primero, o utilice desactivación lógica."));
        }

        _context.Aulas.Remove(aula);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
