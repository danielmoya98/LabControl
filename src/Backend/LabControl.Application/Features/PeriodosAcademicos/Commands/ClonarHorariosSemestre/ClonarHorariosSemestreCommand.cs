using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;

namespace LabControl.Application.Features.PeriodosAcademicos.Commands.ClonarHorariosSemestre;

public record ClonarHorariosSemestreCommand(
    int PeriodoOrigenId,
    int PeriodoDestinoId
) : IRequest<Result<int>>;

public class ClonarHorariosSemestreCommandValidator : AbstractValidator<ClonarHorariosSemestreCommand>
{
    public ClonarHorariosSemestreCommandValidator()
    {
        RuleFor(x => x.PeriodoOrigenId).GreaterThan(0).WithMessage("Debe especificar un período origen válido.");
        RuleFor(x => x.PeriodoDestinoId).GreaterThan(0).WithMessage("Debe especificar un período destino válido.");
        RuleFor(x => x)
            .Must(x => x.PeriodoOrigenId != x.PeriodoDestinoId)
            .WithMessage("El período origen y destino no pueden ser el mismo.");
    }
}

public class ClonarHorariosSemestreCommandHandler : IRequestHandler<ClonarHorariosSemestreCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;

    public ClonarHorariosSemestreCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(ClonarHorariosSemestreCommand request, CancellationToken cancellationToken)
    {
        var origenExiste = await _context.PeriodosAcademicos.AnyAsync(p => p.Id == request.PeriodoOrigenId, cancellationToken);
        if (!origenExiste)
        {
            return Result<int>.Failure(Error.NotFound("PeriodoAcademico.NotFound", "El período origen no existe."));
        }

        var destino = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == request.PeriodoDestinoId, cancellationToken);
        if (destino == null)
        {
            return Result<int>.Failure(Error.NotFound("PeriodoAcademico.NotFound", "El período destino no existe."));
        }

        var bloquesOrigen = await _context.BloquesHorarios
            .Where(b => b.PeriodoAcademicoId == request.PeriodoOrigenId)
            .ToListAsync(cancellationToken);

        if (!bloquesOrigen.Any())
        {
            return Result<int>.Failure(Error.Validation("ClonarHorarios.EmptySource", "El período origen no contiene bloques horarios para clonar."));
        }

        int clonados = 0;
        foreach (var bo in bloquesOrigen)
        {
            var nuevoBloqueResult = BloqueHorario.Create(
                bo.AulaId,
                bo.DiaSemana,
                bo.HoraInicio,
                bo.HoraFin,
                bo.EsRecreo,
                bo.Descripcion,
                bo.MateriaId,
                bo.DocenteId,
                bo.GrupoParalelo,
                bo.EsUsoLibre,
                request.PeriodoDestinoId,
                bo.DocenteNombreManual,
                bo.DocenteEmailManual,
                bo.MateriaNombreManual
            );

            if (nuevoBloqueResult.IsSuccess)
            {
                _context.BloquesHorarios.Add(nuevoBloqueResult.Value);
                clonados++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(clonados);
    }
}
