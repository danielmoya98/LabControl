using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Application.Features.Materias.Queries.GetMaterias;

namespace LabControl.Application.Features.Materias.Commands.UpdateMateria;

public record UpdateMateriaCommand(
    int Id,
    string Sigla,
    string Nombre,
    string? Carrera,
    bool Activo = true
) : IRequest<Result<MateriaDto>>;

public class UpdateMateriaCommandValidator : AbstractValidator<UpdateMateriaCommand>
{
    public UpdateMateriaCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id de materia inválido.");
        RuleFor(x => x.Sigla).NotEmpty().WithMessage("La sigla de la materia es requerida.");
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre de la materia es requerido.");
    }
}

public class UpdateMateriaCommandHandler : IRequestHandler<UpdateMateriaCommand, Result<MateriaDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateMateriaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MateriaDto>> Handle(UpdateMateriaCommand request, CancellationToken cancellationToken)
    {
        var materia = await _context.Materias
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (materia == null)
        {
            return Result<MateriaDto>.Failure(Error.NotFound("Materia.NotFound", "No se encontró la materia especificada."));
        }

        materia.Update(request.Sigla, request.Nombre, request.Carrera);
        if (request.Activo)
        {
            materia.Activar();
        }
        else
        {
            materia.Desactivar();
        }

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
