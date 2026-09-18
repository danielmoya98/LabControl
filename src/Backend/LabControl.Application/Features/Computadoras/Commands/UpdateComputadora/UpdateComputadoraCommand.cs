using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Computadoras.Commands.CreateComputadora;
using LabControl.Domain.Common;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Application.Features.Computadoras.Commands.UpdateComputadora;

public record UpdateComputadoraCommand(
    int Id,
    int AulaId,
    string Hostname,
    string IpActual,
    string MacAddress,
    EstadoComputadora? EstadoActual = null
) : IRequest<Result<ComputadoraCreatedDto>>;

public class UpdateComputadoraCommandValidator : AbstractValidator<UpdateComputadoraCommand>
{
    public UpdateComputadoraCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la computadora es inválido.");
        RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("Seleccione un aula válida.");
        RuleFor(x => x.Hostname).NotEmpty().WithMessage("El Hostname es requerido.");
        RuleFor(x => x.IpActual).NotEmpty().WithMessage("La IP es requerida.");
        RuleFor(x => x.MacAddress).NotEmpty().WithMessage("La dirección MAC es requerida.");
    }
}

public class UpdateComputadoraCommandHandler : IRequestHandler<UpdateComputadoraCommand, Result<ComputadoraCreatedDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISignalRNotificationService _signalRService;

    public UpdateComputadoraCommandHandler(
        IApplicationDbContext context,
        ISignalRNotificationService signalRService)
    {
        _context = context;
        _signalRService = signalRService;
    }

    public async Task<Result<ComputadoraCreatedDto>> Handle(UpdateComputadoraCommand request, CancellationToken cancellationToken)
    {
        var computadora = await _context.Computadoras
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (computadora == null)
        {
            return Result<ComputadoraCreatedDto>.Failure(Error.NotFound("Computadora.NotFound", $"No se encontró la computadora con ID {request.Id}."));
        }

        // Verificar que el aula destino exista
        var aulaExiste = await _context.Aulas.AnyAsync(a => a.Id == request.AulaId, cancellationToken);
        if (!aulaExiste)
        {
            return Result<ComputadoraCreatedDto>.Failure(Error.NotFound("Aula.NotFound", $"El aula con ID {request.AulaId} no existe."));
        }

        var ipResult = IpAddress.Create(request.IpActual);
        if (ipResult.IsFailure) return Result<ComputadoraCreatedDto>.Failure(ipResult.Error);

        var macResult = MacAddress.Create(request.MacAddress);
        if (macResult.IsFailure) return Result<ComputadoraCreatedDto>.Failure(macResult.Error);

        var hostNorm = request.Hostname.Trim().ToUpperInvariant();

        // Validar colisión de Hostname o MAC con otra PC distinta
        var colision = await _context.Computadoras
            .AnyAsync(c => c.Id != request.Id && (c.Hostname == hostNorm || c.MacAddress == macResult.Value.Value), cancellationToken);

        if (colision)
        {
            return Result<ComputadoraCreatedDto>.Failure(Error.Conflict("Computadora.DuplicateHardware", "Ya existe otra computadora con el mismo Hostname o Dirección MAC."));
        }

        computadora.Update(request.AulaId, hostNorm, ipResult.Value, macResult.Value, request.EstadoActual);
        await _context.SaveChangesAsync(cancellationToken);

        // Notificar cambio por SignalR
        await _signalRService.NotifyEstadoComputadoraCambiadoAsync(
            computadora.Id,
            computadora.Hostname,
            computadora.EstadoActual,
            null,
            cancellationToken);

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
