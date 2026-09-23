using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Aulas.Commands.CreateAula;
using LabControl.Domain.Common;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Aulas.Commands.UpdateAula;

public record UpdateAulaCommand(
    int Id,
    string Nombre,
    int Capacidad,
    string? Pabellon = null,
    bool Activo = true,
    int MinutosInactividad = 15,
    TipoAccionInactividad AccionInactividad = TipoAccionInactividad.ApagarEquipo,
    int? BloqueId = null,
    string? Piso = null
) : IRequest<Result<AulaDto>>;

public class UpdateAulaCommandValidator : AbstractValidator<UpdateAulaCommand>
{
    public UpdateAulaCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador del aula es inválido.");
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre del aula es requerido.");
        RuleFor(x => x.Capacidad).GreaterThan(0).WithMessage("La capacidad debe ser mayor a 0.");
    }
}

public class UpdateAulaCommandHandler : IRequestHandler<UpdateAulaCommand, Result<AulaDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateAulaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AulaDto>> Handle(UpdateAulaCommand request, CancellationToken cancellationToken)
    {
        var aula = await _context.Aulas
            .Include(a => a.Computadoras)
            .Include(a => a.Bloque)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (aula == null)
        {
            return Result<AulaDto>.Failure(Error.NotFound("Aula.NotFound", $"No se encontró el aula con ID {request.Id}."));
        }

        // Verificar si ya existe otra aula con el mismo nombre en el mismo bloque
        var nombreExiste = await _context.Aulas
            .AnyAsync(a => a.Id != request.Id && a.Nombre.ToLower() == request.Nombre.Trim().ToLower(), cancellationToken);

        if (nombreExiste)
        {
            return Result<AulaDto>.Failure(Error.Conflict("Aula.DuplicateName", $"Ya existe otra aula con el nombre '{request.Nombre}'."));
        }

        aula.Update(
            request.Nombre, 
            request.Capacidad, 
            request.Pabellon, 
            request.MinutosInactividad, 
            request.AccionInactividad,
            request.BloqueId,
            request.Piso
        );

        if (request.Activo)
        {
            aula.Activar();
        }
        else
        {
            aula.Desactivar();
        }

        await _context.SaveChangesAsync(cancellationToken);

        string? bloqueNombre = aula.Bloque?.Nombre;
        if (string.IsNullOrEmpty(bloqueNombre) && aula.BloqueId.HasValue)
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
            aula.Computadoras.Count,
            aula.MinutosInactividadMaximo,
            aula.AccionInactividad,
            aula.BloqueId,
            bloqueNombre,
            aula.Piso
        ));
    }
}
