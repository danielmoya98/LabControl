using FluentValidation;
using MediatR;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.ValueObjects;

namespace LabControl.Application.Features.Computadoras.Commands.CreateComputadora;

public record CreateComputadoraCommand(
    int AulaId,
    string Hostname,
    string IpActual,
    string MacAddress
) : IRequest<Result<ComputadoraCreatedDto>>;

public record ComputadoraCreatedDto(
    int Id,
    int AulaId,
    string Hostname,
    string IpActual,
    string MacAddress,
    string EstadoActual
);

public class CreateComputadoraCommandValidator : AbstractValidator<CreateComputadoraCommand>
{
    public CreateComputadoraCommandValidator()
    {
        RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("Seleccione un aula válida.");
        RuleFor(x => x.Hostname).NotEmpty().WithMessage("El Hostname es requerido.");
        RuleFor(x => x.IpActual).NotEmpty().WithMessage("La IP es requerida.");
        RuleFor(x => x.MacAddress).NotEmpty().WithMessage("La dirección MAC es requerida.");
    }
}

public class CreateComputadoraCommandHandler : IRequestHandler<CreateComputadoraCommand, Result<ComputadoraCreatedDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateComputadoraCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ComputadoraCreatedDto>> Handle(CreateComputadoraCommand request, CancellationToken cancellationToken)
    {
        var ipResult = IpAddress.Create(request.IpActual);
        if (ipResult.IsFailure) return Result<ComputadoraCreatedDto>.Failure(ipResult.Error);

        var macResult = MacAddress.Create(request.MacAddress);
        if (macResult.IsFailure) return Result<ComputadoraCreatedDto>.Failure(macResult.Error);

        var pcResult = Computadora.Create(request.AulaId, request.Hostname, ipResult.Value, macResult.Value);
        if (pcResult.IsFailure) return Result<ComputadoraCreatedDto>.Failure(pcResult.Error);

        var computadora = pcResult.Value;
        _context.Computadoras.Add(computadora);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ComputadoraCreatedDto>.Success(new ComputadoraCreatedDto(
            computadora.Id,
            computadora.AulaId,
            computadora.Hostname,
            computadora.IpActual,
            computadora.MacAddress,
            computadora.EstadoActual.ToString()
        ));
    }
}
