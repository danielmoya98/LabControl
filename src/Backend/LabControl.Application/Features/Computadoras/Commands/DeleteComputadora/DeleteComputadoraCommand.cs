using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Computadoras.Commands.DeleteComputadora;

public record DeleteComputadoraCommand(int Id) : IRequest<Result<bool>>;

public class DeleteComputadoraCommandValidator : AbstractValidator<DeleteComputadoraCommand>
{
    public DeleteComputadoraCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la computadora es inválido.");
    }
}

public class DeleteComputadoraCommandHandler : IRequestHandler<DeleteComputadoraCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISignalRNotificationService _signalRService;

    public DeleteComputadoraCommandHandler(
        IApplicationDbContext context,
        ISignalRNotificationService signalRService)
    {
        _context = context;
        _signalRService = signalRService;
    }

    public async Task<Result<bool>> Handle(DeleteComputadoraCommand request, CancellationToken cancellationToken)
    {
        var computadora = await _context.Computadoras
            .Include(c => c.SesionesUso)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (computadora == null)
        {
            return Result<bool>.Failure(Error.NotFound("Computadora.NotFound", $"No se encontró la computadora con ID {request.Id}."));
        }

        // Si tiene una sesión activa, cerrarla antes de eliminar
        var sesionActiva = computadora.SesionesUso.FirstOrDefault(s => s.FechaHoraFin == null);
        if (sesionActiva != null)
        {
            sesionActiva.Finalizar(TipoCierreSesion.AdminRemoto, DateTime.UtcNow);
        }

        _context.Computadoras.Remove(computadora);
        await _context.SaveChangesAsync(cancellationToken);

        // Notificar por SignalR que la terminal fue removida
        await _signalRService.NotifyEstadoComputadoraCambiadoAsync(
            computadora.Id,
            computadora.Hostname,
            EstadoComputadora.Offline,
            null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
