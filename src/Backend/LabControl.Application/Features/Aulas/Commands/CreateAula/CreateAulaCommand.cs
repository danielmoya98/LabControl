using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Aulas.Commands.CreateAula;

public record CreateAulaCommand(
    string Nombre,
    int Capacidad,
    string? Pabellon = null,
    int MinutosInactividad = 15,
    TipoAccionInactividad AccionInactividad = TipoAccionInactividad.ApagarEquipo,
    int? BloqueId = null,
    string? Piso = null
) : IRequest<Result<AulaDto>>;

public record AulaDto(
    int Id,
    string Nombre,
    int Capacidad,
    string? Pabellon,
    bool Activo,
    int TotalComputadoras,
    int MinutosInactividadMaximo = 15,
    TipoAccionInactividad AccionInactividad = TipoAccionInactividad.ApagarEquipo,
    int? BloqueId = null,
    string? BloqueNombre = null,
    string? Piso = null
);

public class CreateAulaCommandValidator : AbstractValidator<CreateAulaCommand>
{
    public CreateAulaCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre del aula es requerido.");
        RuleFor(x => x.Capacidad).GreaterThan(0).WithMessage("La capacidad debe ser mayor a 0.");
    }
}

public class CreateAulaCommandHandler : IRequestHandler<CreateAulaCommand, Result<AulaDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateAulaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AulaDto>> Handle(CreateAulaCommand request, CancellationToken cancellationToken)
    {
        var aulaResult = Aula.Create(
            request.Nombre, 
            request.Capacidad, 
            request.Pabellon, 
            request.MinutosInactividad, 
            request.AccionInactividad,
            request.BloqueId,
            request.Piso
        );

        if (aulaResult.IsFailure)
        {
            return Result<AulaDto>.Failure(aulaResult.Error);
        }

        var aula = aulaResult.Value;
        _context.Aulas.Add(aula);
        await _context.SaveChangesAsync(cancellationToken);

        string? bloqueNombre = null;
        if (aula.BloqueId.HasValue)
        {
            var b = await _context.Bloques.FindAsync([aula.BloqueId.Value], cancellationToken);
            bloqueNombre = b?.Nombre;
        }

        return Result<AulaDto>.Success(new AulaDto(
            aula.Id,
            aula.Nombre,
            aula.Capacidad,
            aula.Pabellon,
            aula.Activo,
            0,
            aula.MinutosInactividadMaximo,
            aula.AccionInactividad,
            aula.BloqueId,
            bloqueNombre,
            aula.Piso
        ));
    }
}
