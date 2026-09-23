using FluentValidation;
using MediatR;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Application.Features.Docentes.Queries.GetDocentes;

namespace LabControl.Application.Features.Docentes.Commands.CreateDocente;

public record CreateDocenteCommand(
    string Nombres,
    string Apellidos,
    string EmailInstitucional,
    string? TelefonoContacto
) : IRequest<Result<DocenteDto>>;

public class CreateDocenteCommandValidator : AbstractValidator<CreateDocenteCommand>
{
    public CreateDocenteCommandValidator()
    {
        RuleFor(x => x.Nombres).NotEmpty().WithMessage("Los nombres son requeridos.");
        RuleFor(x => x.Apellidos).NotEmpty().WithMessage("Los apellidos son requeridos.");
        RuleFor(x => x.EmailInstitucional).NotEmpty().EmailAddress().WithMessage("El correo electrónico es inválido.");
    }
}

public class CreateDocenteCommandHandler : IRequestHandler<CreateDocenteCommand, Result<DocenteDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateDocenteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DocenteDto>> Handle(CreateDocenteCommand request, CancellationToken cancellationToken)
    {
        var docenteResult = Docente.Create(
            request.Nombres,
            request.Apellidos,
            request.EmailInstitucional,
            request.TelefonoContacto
        );

        if (docenteResult.IsFailure)
        {
            return Result<DocenteDto>.Failure(docenteResult.Error);
        }

        var docente = docenteResult.Value;
        _context.Docentes.Add(docente);
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
