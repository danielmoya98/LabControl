using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;

public record CreateBloqueHorarioCommand(
    int AulaId,
    DiaSemana DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    bool EsRecreo,
    string? Descripcion,
    int? MateriaId = null,
    int? DocenteId = null,
    string? GrupoParalelo = null,
    bool EsUsoLibre = false,
    int? PeriodoAcademicoId = null,
    string? DocenteNombreManual = null,
    string? DocenteEmailManual = null,
    string? MateriaNombreManual = null
) : IRequest<Result<BloqueHorarioDto>>;

public record BloqueHorarioDto(
    int Id,
    int AulaId,
    DiaSemana DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    bool EsRecreo,
    string? Descripcion,
    int? MateriaId = null,
    string? MateriaSigla = null,
    string? MateriaNombre = null,
    int? DocenteId = null,
    string? DocenteNombre = null,
    string? GrupoParalelo = null,
    bool EsUsoLibre = false,
    int? PeriodoAcademicoId = null,
    string? PeriodoNombre = null,
    string? DocenteNombreManual = null,
    string? DocenteEmailManual = null,
    string? MateriaNombreManual = null
);

public class CreateBloqueHorarioCommandValidator : AbstractValidator<CreateBloqueHorarioCommand>
{
    public CreateBloqueHorarioCommandValidator()
    {
        RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("El aula es requerida.");
        RuleFor(x => x.HoraFin).GreaterThan(x => x.HoraInicio).WithMessage("La hora de fin debe ser mayor a la hora de inicio.");
    }
}

public class CreateBloqueHorarioCommandHandler : IRequestHandler<CreateBloqueHorarioCommand, Result<BloqueHorarioDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateBloqueHorarioCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BloqueHorarioDto>> Handle(CreateBloqueHorarioCommand request, CancellationToken cancellationToken)
    {
        int? periodoId = request.PeriodoAcademicoId;
        string? periodoNombre = null;

        if (periodoId.HasValue && periodoId.Value > 0)
        {
            var p = await _context.PeriodosAcademicos.FindAsync([periodoId.Value], cancellationToken);
            periodoNombre = p?.Nombre;
        }
        else
        {
            var pActivo = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.EsActual, cancellationToken);
            if (pActivo != null)
            {
                periodoId = pActivo.Id;
                periodoNombre = pActivo.Nombre;
            }
        }

        var bloqueResult = BloqueHorario.Create(
            request.AulaId,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFin,
            request.EsRecreo,
            request.Descripcion,
            request.MateriaId,
            request.DocenteId,
            request.GrupoParalelo,
            request.EsUsoLibre,
            periodoId,
            request.DocenteNombreManual,
            request.DocenteEmailManual,
            request.MateriaNombreManual
        );

        if (bloqueResult.IsFailure) return Result<BloqueHorarioDto>.Failure(bloqueResult.Error);

        var bloque = bloqueResult.Value;
        _context.BloquesHorarios.Add(bloque);
        await _context.SaveChangesAsync(cancellationToken);

        string? materiaSigla = null;
        string? materiaNombre = bloque.MateriaNombreManual;
        if (bloque.MateriaId.HasValue)
        {
            var mat = await _context.Materias.FindAsync([bloque.MateriaId.Value], cancellationToken);
            materiaSigla = mat?.Sigla;
            materiaNombre = mat?.Nombre ?? materiaNombre;
        }

        string? docenteNombre = bloque.DocenteNombreManual;
        if (bloque.DocenteId.HasValue)
        {
            var doc = await _context.Docentes.FindAsync([bloque.DocenteId.Value], cancellationToken);
            docenteNombre = doc?.NombreCompleto ?? docenteNombre;
        }

        return Result<BloqueHorarioDto>.Success(new BloqueHorarioDto(
            bloque.Id,
            bloque.AulaId,
            bloque.DiaSemana,
            bloque.HoraInicio,
            bloque.HoraFin,
            bloque.EsRecreo,
            bloque.Descripcion,
            bloque.MateriaId,
            materiaSigla,
            materiaNombre,
            bloque.DocenteId,
            docenteNombre,
            bloque.GrupoParalelo,
            bloque.EsUsoLibre,
            bloque.PeriodoAcademicoId,
            periodoNombre,
            bloque.DocenteNombreManual,
            bloque.DocenteEmailManual,
            bloque.MateriaNombreManual
        ));
    }
}
