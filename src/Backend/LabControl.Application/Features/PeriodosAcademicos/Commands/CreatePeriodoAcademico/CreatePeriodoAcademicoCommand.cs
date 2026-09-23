using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.PeriodosAcademicos.Queries.GetPeriodosAcademicos;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;

namespace LabControl.Application.Features.PeriodosAcademicos.Commands.CreatePeriodoAcademico;

public record CreatePeriodoAcademicoCommand(
    string Nombre,
    DateTime FechaInicio,
    DateTime FechaFin,
    bool EsActual = false
) : IRequest<Result<PeriodoAcademicoDto>>;

public class CreatePeriodoAcademicoCommandValidator : AbstractValidator<CreatePeriodoAcademicoCommand>
{
    public CreatePeriodoAcademicoCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del período es requerido.")
            .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");

        RuleFor(x => x.FechaFin)
            .GreaterThan(x => x.FechaInicio)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");
    }
}

public class CreatePeriodoAcademicoCommandHandler : IRequestHandler<CreatePeriodoAcademicoCommand, Result<PeriodoAcademicoDto>>
{
    private readonly IApplicationDbContext _context;

    public CreatePeriodoAcademicoCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PeriodoAcademicoDto>> Handle(CreatePeriodoAcademicoCommand request, CancellationToken cancellationToken)
    {
        string nombreLimpio = request.Nombre.Trim().ToUpperInvariant();

        var existe = await _context.PeriodosAcademicos
            .AnyAsync(p => p.Nombre == nombreLimpio, cancellationToken);

        if (existe)
        {
            return Result<PeriodoAcademicoDto>.Failure(
                Error.Conflict("PeriodoAcademico.Duplicate", $"Ya existe un período académico con el nombre '{nombreLimpio}'."));
        }

        if (request.EsActual)
        {
            // Desmarcar otros períodos activos
            var periodosActuales = await _context.PeriodosAcademicos
                .Where(p => p.EsActual)
                .ToListAsync(cancellationToken);

            foreach (var p in periodosActuales)
            {
                p.DesmarcarActual();
            }
        }

        var periodoResult = PeriodoAcademico.Create(
            nombreLimpio,
            request.FechaInicio,
            request.FechaFin,
            request.EsActual
        );

        if (periodoResult.IsFailure)
        {
            return Result<PeriodoAcademicoDto>.Failure(periodoResult.Error);
        }

        var periodo = periodoResult.Value;
        _context.PeriodosAcademicos.Add(periodo);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<PeriodoAcademicoDto>.Success(new PeriodoAcademicoDto(
            periodo.Id,
            periodo.Nombre,
            periodo.FechaInicio,
            periodo.FechaFin,
            periodo.EsActual,
            periodo.Activo,
            0
        ));
    }
}
