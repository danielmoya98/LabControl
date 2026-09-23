using FluentValidation;
using MediatR;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Application.Features.Materias.Queries.GetMaterias;

namespace LabControl.Application.Features.Materias.Commands.CreateMateria;

public record CreateMateriaCommand(
    string Sigla,
    string Nombre,
    string? Carrera
) : IRequest<Result<MateriaDto>>;

public class CreateMateriaCommandValidator : AbstractValidator<CreateMateriaCommand>
{
    public CreateMateriaCommandValidator()
    {
        RuleFor(x => x.Sigla).NotEmpty().WithMessage("La sigla de la materia es requerida.");
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre de la materia es requerido.");
    }
}

public class CreateMateriaCommandHandler : IRequestHandler<CreateMateriaCommand, Result<MateriaDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateMateriaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MateriaDto>> Handle(CreateMateriaCommand request, CancellationToken cancellationToken)
    {
        var materiaResult = Materia.Create(
            request.Sigla,
            request.Nombre,
            request.Carrera
        );

        if (materiaResult.IsFailure)
        {
            return Result<MateriaDto>.Failure(materiaResult.Error);
        }

        var materia = materiaResult.Value;
        _context.Materias.Add(materia);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<MateriaDto>.Success(new MateriaDto(
            materia.Id,
            materia.Sigla,
            materia.Nombre,
            materia.Carrera,
            materia.Activo
        ));
    }
}
