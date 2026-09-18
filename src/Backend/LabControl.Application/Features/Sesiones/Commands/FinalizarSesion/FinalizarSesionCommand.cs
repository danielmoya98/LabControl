using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Sesiones.Commands.FinalizarSesion;

public record FinalizarSesionResponse(
    int SesionId,
    int DuracionMinutos,
    DateTime FechaHoraFin,
    TipoCierreSesion TipoCierre
);

public record FinalizarSesionCommand(
    int? SesionId,
    int? ComputadoraId,
    string? Hostname,
    TipoCierreSesion TipoCierre = TipoCierreSesion.Manual
) : IRequest<Result<FinalizarSesionResponse>>;

public class FinalizarSesionCommandHandler : IRequestHandler<FinalizarSesionCommand, Result<FinalizarSesionResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISignalRNotificationService _signalRService;

    public FinalizarSesionCommandHandler(
        IApplicationDbContext context,
        ISignalRNotificationService signalRService)
    {
        _context = context;
        _signalRService = signalRService;
    }

    public async Task<Result<FinalizarSesionResponse>> Handle(FinalizarSesionCommand request, CancellationToken cancellationToken)
    {
        SesionUso? sesion = null;

        // 1. Buscar sesión por ID si fue provisto
        if (request.SesionId.HasValue && request.SesionId.Value > 0)
        {
            sesion = await _context.SesionesUso
                .Include(s => s.Computadora)
                .FirstOrDefaultAsync(s => s.Id == request.SesionId.Value, cancellationToken);
        }

        // 2. Si no se encontró por ID, buscar la sesión activa por ComputadoraId o Hostname
        if (sesion == null)
        {
            IQueryable<SesionUso> query = _context.SesionesUso
                .Include(s => s.Computadora)
                .Where(s => s.FechaHoraFin == null);

            if (request.ComputadoraId.HasValue && request.ComputadoraId.Value > 0)
            {
                query = query.Where(s => s.ComputadoraId == request.ComputadoraId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.Hostname))
            {
                var hostNorm = request.Hostname.Trim().ToUpperInvariant();
                query = query.Where(s => s.Computadora.Hostname == hostNorm);
            }

            sesion = await query.OrderByDescending(s => s.FechaHoraInicio).FirstOrDefaultAsync(cancellationToken);
        }

        if (sesion == null)
        {
            // Si no hay sesión activa, al menos asegurar que la computadora quede disponible si se especificó
            if (!string.IsNullOrWhiteSpace(request.Hostname) || (request.ComputadoraId.HasValue && request.ComputadoraId.Value > 0))
            {
                var pc = await _context.Computadoras
                    .FirstOrDefaultAsync(c => 
                        (request.ComputadoraId.HasValue && c.Id == request.ComputadoraId.Value) || 
                        (!string.IsNullOrWhiteSpace(request.Hostname) && c.Hostname == request.Hostname.Trim().ToUpperInvariant()), cancellationToken);

                if (pc != null)
                {
                    pc.CambiarEstado(EstadoComputadora.Disponible);
                    await _context.SaveChangesAsync(cancellationToken);
                    await _signalRService.NotifyEstadoComputadoraCambiadoAsync(pc.Id, pc.Hostname, EstadoComputadora.Disponible, null, cancellationToken);
                }
            }

            return Result<FinalizarSesionResponse>.Failure(
                Error.NotFound("SesionUso.NotFound", "No se encontró una sesión activa para finalizar."));
        }

        // 3. Finalizar sesión
        sesion.Finalizar(request.TipoCierre, DateTime.UtcNow);

        // 4. Liberar computadora y registrar trazabilidad del último usuario
        if (sesion.Computadora != null)
        {
            sesion.Computadora.CambiarEstado(EstadoComputadora.Disponible);
            sesion.Computadora.RegistrarUltimoUsuario(sesion.EmailEstudiante);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Notificar por SignalR a los paneles administrativos
        if (sesion.Computadora != null)
        {
            await _signalRService.NotifyEstadoComputadoraCambiadoAsync(
                sesion.Computadora.Id,
                sesion.Computadora.Hostname,
                EstadoComputadora.Disponible,
                null,
                cancellationToken);
        }

        var response = new FinalizarSesionResponse(
            sesion.Id,
            sesion.DuracionMinutos ?? 0,
            sesion.FechaHoraFin ?? DateTime.UtcNow,
            sesion.TipoCierre
        );

        return Result<FinalizarSesionResponse>.Success(response);
    }
}
