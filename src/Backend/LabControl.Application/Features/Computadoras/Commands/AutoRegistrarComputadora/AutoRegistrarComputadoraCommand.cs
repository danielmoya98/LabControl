using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Application.Features.Computadoras.Commands.AutoRegistrarComputadora;

public record AutoRegistrarComputadoraCommand(
    int AulaId,
    string Hostname,
    string IpActual,
    string MacAddress,
    string? CpuModelo = null,
    int? RamTotalGb = null,
    int? DiscoTotalGb = null,
    int? DiscoLibreGb = null,
    string? SistemaOperativo = null,
    double? UptimeHoras = null
) : IRequest<Result<AutoRegistroResultadoDto>>;

public record AutoRegistroResultadoDto(
    int ComputadoraId,
    int AulaId,
    string Hostname,
    string IpActual,
    string MacAddress,
    string EstadoActual
);

public class AutoRegistrarComputadoraCommandValidator : AbstractValidator<AutoRegistrarComputadoraCommand>
{
    public AutoRegistrarComputadoraCommandValidator()
    {
        RuleFor(x => x.AulaId).GreaterThan(0).WithMessage("Seleccione un aula válida.");
        RuleFor(x => x.Hostname).NotEmpty().WithMessage("El Hostname es requerido.");
        RuleFor(x => x.IpActual).NotEmpty().WithMessage("La dirección IP es requerida.");
        RuleFor(x => x.MacAddress).NotEmpty().WithMessage("La dirección MAC es requerida.");
    }
}

public class AutoRegistrarComputadoraCommandHandler : IRequestHandler<AutoRegistrarComputadoraCommand, Result<AutoRegistroResultadoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISignalRNotificationService _notificationService;

    public AutoRegistrarComputadoraCommandHandler(
        IApplicationDbContext context,
        ISignalRNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<AutoRegistroResultadoDto>> Handle(AutoRegistrarComputadoraCommand request, CancellationToken cancellationToken)
    {
        var ipResult = IpAddress.Create(request.IpActual);
        if (ipResult.IsFailure) return Result<AutoRegistroResultadoDto>.Failure(ipResult.Error);

        var macResult = MacAddress.Create(request.MacAddress);
        if (macResult.IsFailure) return Result<AutoRegistroResultadoDto>.Failure(macResult.Error);

        // Buscar si ya existe por MACAddress o Hostname
        var computadora = await _context.Computadoras
            .FirstOrDefaultAsync(c => c.MacAddress == request.MacAddress || c.Hostname == request.Hostname, cancellationToken);

        if (computadora != null)
        {
            // Actualizar datos de la PC existente
            computadora.ActualizarUbicacionRed(ipResult.Value, macResult.Value);
            if (computadora.AulaId != request.AulaId)
            {
                // Reasignar aula si cambió
                computadora.AsignarAula(request.AulaId);
            }
        }
        else
        {
            // Crear nueva PC
            var pcResult = Computadora.Create(request.AulaId, request.Hostname, ipResult.Value, macResult.Value);
            if (pcResult.IsFailure) return Result<AutoRegistroResultadoDto>.Failure(pcResult.Error);

            computadora = pcResult.Value;
            _context.Computadoras.Add(computadora);
        }

        // Actualizar especificaciones de hardware y salud reportadas por el agente Kiosk
        computadora.ActualizarEspecificacionesHardware(
            request.CpuModelo,
            request.RamTotalGb,
            request.DiscoTotalGb,
            request.DiscoLibreGb,
            request.SistemaOperativo,
            request.UptimeHoras
        );

        await _context.SaveChangesAsync(cancellationToken);

        // Notificar en tiempo real por SignalR al WebAdmin
        await _notificationService.NotifyEstadoComputadoraCambiadoAsync(
            computadora.Id,
            computadora.Hostname,
            computadora.EstadoActual,
            null,
            cancellationToken
        );

        return Result<AutoRegistroResultadoDto>.Success(new AutoRegistroResultadoDto(
            computadora.Id,
            computadora.AulaId,
            computadora.Hostname,
            computadora.IpActual,
            computadora.MacAddress,
            computadora.EstadoActual.ToString()
        ));
    }
}
