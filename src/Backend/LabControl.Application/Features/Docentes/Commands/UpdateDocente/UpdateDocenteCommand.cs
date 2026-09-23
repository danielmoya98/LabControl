using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Application.Features.Docentes.Queries.GetDocentes;

namespace LabControl.Application.Features.Docentes.Commands.UpdateDocente;

public record UpdateDocenteCommand(
    int Id,
    string Nombres,
    string Apellidos,
    string EmailInstitucional,
    string? TelefonoContacto,
    bool Activo = true
) : IRequest<Result<DocenteDto>>;

public class UpdateDocenteCommandValidator : AbstractValidator<UpdateDocenteCommand>
{
    public UpdateDocenteCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id de docente inválido.");
        RuleFor(x => x.Nombres).NotEmpty().WithMessage("Los nombres son requeridos.");
        RuleFor(x => x.Apellidos).NotEmpty().WithMessage("Los apellidos son requeridos.");
        RuleFor(x => x.EmailInstitucional).NotEmpty().EmailAddress().WithMessage("Correo institucional inválido.");
    }
}

public class UpdateDocenteCommandHandler : IRequestHandler<UpdateDocenteCommand, Result<DocenteDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateDocenteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DocenteDto>> Handle(UpdateDocenteCommand request, CancellationToken cancellationToken)
    {
        var docente = await _context.Docentes
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (docente == null)
        {
            return Result<DocenteDto>.Failure(Error.NotFound("Docente.NotFound", "No se encontró el docente especificado."));
        }

        docente.Update(request.Nombres, request.Apellidos, request.EmailInstitucional, request.TelefonoContacto);
        if (request.Activo)
        {
            docente.Activar();
        }
        else
        {
            docente.Desactivar();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<DocenteDto>.Success(new DocenteDto(
            docente.Id,
            docente.Nombres,
            docente.Apellidos,
            docente.NombreCompleto,
            docente.EmailInstitucional,
            docente.TelefonoContacto,
            docente.Activo
        ));
    }
}
