using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Application.Features.Sesiones.Commands.IniciarSesion;

public record IniciarSesionResponse(
    int SesionId,
    int ComputadoraId,
    string Hostname,
    string EmailEstudiante,
    DateTime FechaHoraInicio,
    int MinutosLimite
);

public record IniciarSesionCommand(
    int? ComputadoraId,
    string? Hostname,
    string EmailEstudiante,
    int MinutosLimite = 90
) : IRequest<Result<IniciarSesionResponse>>;

public class IniciarSesionCommandValidator : AbstractValidator<IniciarSesionCommand>
{
    public IniciarSesionCommandValidator()
    {
        RuleFor(x => x.EmailEstudiante)
            .NotEmpty().WithMessage("El correo institucional es obligatorio.")
            .Matches(@"^[a-zA-Z0-9._%+-]+@(est\.univalle\.edu|univalle\.edu)$")
            .WithMessage("El correo debe pertenecer al dominio institucional (@est.univalle.edu o @univalle.edu).");
    }
}

public class IniciarSesionCommandHandler : IRequestHandler<IniciarSesionCommand, Result<IniciarSesionResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISignalRNotificationService _signalRService;

    public IniciarSesionCommandHandler(
        IApplicationDbContext context,
        ISignalRNotificationService signalRService)
    {
        _context = context;
        _signalRService = signalRService;
    }

    public async Task<Result<IniciarSesionResponse>> Handle(IniciarSesionCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar correo institucional
        var emailResult = EmailInstitucional.Create(request.EmailEstudiante);
        if (emailResult.IsFailure)
        {
            return Result<IniciarSesionResponse>.Failure(emailResult.Error);
        }

        // 2. Buscar la computadora por Id o Hostname
        Computadora? computadora = null;
        if (request.ComputadoraId.HasValue && request.ComputadoraId.Value > 0)
        {
            computadora = await _context.Computadoras
                .FirstOrDefaultAsync(c => c.Id == request.ComputadoraId.Value, cancellationToken);
        }

        if (computadora == null && !string.IsNullOrWhiteSpace(request.Hostname))
        {
            var hostNorm = request.Hostname.Trim().ToUpperInvariant();
            computadora = await _context.Computadoras
                .FirstOrDefaultAsync(c => c.Hostname == hostNorm, cancellationToken);
        }

        if (computadora == null)
        {
            return Result<IniciarSesionResponse>.Failure(
                Error.NotFound("Computadora.NotFound", "No se encontró la computadora especificada en el sistema. Asegúrese de que esté registrada."));
        }

        // 3. Duración estándar de sesión (Acceso 100% libre sin restricción horaria)
        // Las terminales quedan disponibles a cualquier hora para clases regulares, clases de reposición o estudio libre.
        // Los horarios en base de datos se conservan intactos para cruzar datos y generar reportes de asistencia y auditoría.
        int minutosLimiteFinal = request.MinutosLimite > 0 ? request.MinutosLimite : 90;

        // 4. Cerrar cualquier sesión previa abierta en esta computadora (Relevo forzado)
        var sesionesAbiertas = await _context.SesionesUso
            .Where(s => s.ComputadoraId == computadora.Id && s.FechaHoraFin == null)
            .ToListAsync(cancellationToken);

        foreach (var sesionAbierta in sesionesAbiertas)
        {
            sesionAbierta.Finalizar(TipoCierreSesion.RelevoForzado, DateTime.UtcNow);
        }

        // 5. Iniciar nueva sesión
        var nuevaSesionResult = SesionUso.Iniciar(
            computadora.Id,
            emailResult.Value,
            DateTime.UtcNow,
            SyncStatus.Online);

        if (nuevaSesionResult.IsFailure)
        {
            return Result<IniciarSesionResponse>.Failure(nuevaSesionResult.Error);
        }

        var nuevaSesion = nuevaSesionResult.Value;
        _context.SesionesUso.Add(nuevaSesion);

        // 6. Actualizar estado de la computadora y registrar último usuario para auditoría energética
        computadora.CambiarEstado(EstadoComputadora.EnUso);
        computadora.RegistrarUltimoUsuario(emailResult.Value.Value);

        await _context.SaveChangesAsync(cancellationToken);

        // 7. Notificar por SignalR a los paneles administrativos
        await _signalRService.NotifyEstadoComputadoraCambiadoAsync(
            computadora.Id,
            computadora.Hostname,
            EstadoComputadora.EnUso,
            emailResult.Value.Value,
            cancellationToken);

        var response = new IniciarSesionResponse(
            nuevaSesion.Id,
            computadora.Id,
            computadora.Hostname,
            nuevaSesion.EmailEstudiante,
            nuevaSesion.FechaHoraInicio,
            minutosLimiteFinal
        );

        return Result<IniciarSesionResponse>.Success(response);
    }
}
