using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
using LabControl.Domain.Common;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Horarios.Commands.UpdateBloqueHorario;

public record UpdateBloqueHorarioCommand(
    int Id,
    int AulaId,
    DiaSemana DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    bool EsRecreo,
    string? Descripcion
) : IRequest<Result<BloqueHorarioDto>>;

public class UpdateBloqueHorarioCommandValidator : AbstractValidator<UpdateBloqueHorarioCommand>
{
    public UpdateBloqueHorarioCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador del bloque horario es inválido.");
        RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El aula es requerida.");
        RuleFor(x => x.HoraFin).GreaterThan(x => x.HoraInicio).WithMessage("La hora de fin debe ser mayor a la hora de inicio.");
    }
}

public class UpdateBloqueHorarioCommandHandler : IRequestHandler<UpdateBloqueHorarioCommand, Result<BloqueHorarioDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateBloqueHorarioCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BloqueHorarioDto>> Handle(UpdateBloqueHorarioCommand request, CancellationToken cancellationToken)
    {
        var bloque = await _context.BloquesHorarios
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (bloque == null)
        {
            return Result<BloqueHorarioDto>.Failure(Error.NotFound("BloqueHorario.NotFound", $"No se encontró el bloque horario con ID {request.Id}."));
        }

        // Verificar que el aula exista
        var aulaExiste = await _context.Aulas.AnyAsync(a => a.Id == request.AulaId, cancellationToken);
        if (!aulaExiste)
        {
            return Result<BloqueHorarioDto>.Failure(Error.NotFound("Aula.NotFound", $"El aula con ID {request.AulaId} no existe."));
        }

        var updateResult = bloque.Update(
            request.AulaId,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFin,
            request.EsRecreo,
            request.Descripcion
        );

        if (updateResult.IsFailure)
        {
            return Result<BloqueHorarioDto>.Failure(updateResult.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<BloqueHorarioDto>.Success(new BloqueHorarioDto(
            bloque.Id,
            bloque.AulaId,
            bloque.DiaSemana,
            bloque.HoraInicio,
            bloque.HoraFin,
            bloque.EsRecreo,
            bloque.Descripcion
        ));
    }
}
