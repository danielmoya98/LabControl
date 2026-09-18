using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Horarios.Commands.DeleteBloqueHorario;

public record DeleteBloqueHorarioCommand(int Id) : IRequest<Result<bool>>;

public class DeleteBloqueHorarioCommandValidator : AbstractValidator<DeleteBloqueHorarioCommand>
{
    public DeleteBloqueHorarioCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador del bloque horario es inválido.");
    }
}

public class DeleteBloqueHorarioCommandHandler : IRequestHandler<DeleteBloqueHorarioCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteBloqueHorarioCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteBloqueHorarioCommand request, CancellationToken cancellationToken)
    {
        var bloque = await _context.BloquesHorarios
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (bloque == null)
        {
            return Result<bool>.Failure(Error.NotFound("BloqueHorario.NotFound", $"No se encontró el bloque horario con ID {request.Id}."));
        }

        _context.BloquesHorarios.Remove(bloque);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
