using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Application.Features.Bloques.Queries.GetBloques;

namespace LabControl.Application.Features.Bloques.Commands.CreateBloque;

public record CreateBloqueCommand(
    string Nombre,
    string Codigo,
    bool TienePisos,
    int? TotalPisos,
    string? Descripcion
) : IRequest<Result<BloqueDto>>;

public class CreateBloqueCommandValidator : AbstractValidator<CreateBloqueCommand>
{
    public CreateBloqueCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre del bloque es requerido.");
        RuleFor(x => x.Codigo).NotEmpty().WithMessage("El código del bloque es requerido.");
    }
}

public class CreateBloqueCommandHandler : IRequestHandler<CreateBloqueCommand, Result<BloqueDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateBloqueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BloqueDto>> Handle(CreateBloqueCommand request, CancellationToken cancellationToken)
    {
        var sede = await _context.Sedes.FirstOrDefaultAsync(cancellationToken);
        if (sede == null)
        {
            return Result<BloqueDto>.Failure(Error.NotFound("Sede.NotFound", "No hay una sede registrada. Complete el onboarding primero."));
        }

        var bloqueResult = Bloque.Create(
            sede.Id,
            request.Nombre,
            request.Codigo,
            request.TienePisos,
            request.TotalPisos,
            request.Descripcion
        );

        if (bloqueResult.IsFailure)
        {
            return Result<BloqueDto>.Failure(bloqueResult.Error);
        }

        var bloque = bloqueResult.Value;
        _context.Bloques.Add(bloque);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<BloqueDto>.Success(new BloqueDto(
            bloque.Id,
            bloque.SedeId,
            bloque.Nombre,
            bloque.Codigo,
            bloque.TienePisos,
            bloque.TotalPisos,
            bloque.Descripcion,
            bloque.Activo,
            0
        ));
    }
}
