using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Application.Features.Sesiones.Commands.SincronizarSesionesBatch;

public record SesionOfflineDto(
    string EmailEstudiante,
    DateTime FechaHoraInicio,
    DateTime? FechaHoraFin,
    TipoCierreSesion TipoCierre
);

public record SincronizarSesionesBatchCommand(
    string Hostname,
    List<SesionOfflineDto> Sesiones
) : IRequest<Result<int>>;

public class SincronizarSesionesBatchCommandValidator : AbstractValidator<SincronizarSesionesBatchCommand>
{
    public SincronizarSesionesBatchCommandValidator()
    {
        RuleFor(x => x.Hostname).NotEmpty().WithMessage("El hostname es requerido.");
        RuleFor(x => x.Sesiones).NotEmpty().WithMessage("El lote de sesiones no puede estar vacío.");
    }
}

public class SincronizarSesionesBatchCommandHandler : IRequestHandler<SincronizarSesionesBatchCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISignalRNotificationService _notificationService;

    public SincronizarSesionesBatchCommandHandler(
        IApplicationDbContext context,
        ISignalRNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<int>> Handle(SincronizarSesionesBatchCommand request, CancellationToken cancellationToken)
    {
        var hostnameNorm = (request.Hostname ?? "").Trim().ToUpperInvariant();
        var computadora = await _context.Computadoras
            .FirstOrDefaultAsync(c => c.Hostname == hostnameNorm, cancellationToken);

        if (computadora == null)
        {
            var todasAulas = await _context.Aulas.ToListAsync(cancellationToken);
            var coincidente = todasAulas.FirstOrDefault(a => 
                !string.IsNullOrWhiteSpace(a.Nombre) && hostnameNorm.Contains(System.Text.RegularExpressions.Regex.Match(a.Nombre, @"\d+").Value));
            int aulaId = coincidente?.Id ?? todasAulas.FirstOrDefault()?.Id ?? 1;

            var ipResult = IpAddress.Create("127.0.0.1");
            var macResult = MacAddress.Create("00:00:00:00:00:00");
            var pcCreateResult = Computadora.Create(aulaId, hostnameNorm, ipResult.Value, macResult.Value);
            if (pcCreateResult.IsSuccess)
            {
                computadora = pcCreateResult.Value;
                _context.Computadoras.Add(computadora);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                return Result<int>.Failure(Error.NotFound("Computadora.NotFound", $"La computadora {request.Hostname} no existe y no pudo auto-registrarse."));
            }
        }

        int procesados = 0;

        foreach (var dto in request.Sesiones)
        {
            var emailResult = EmailInstitucional.Create(dto.EmailEstudiante);
            if (emailResult.IsFailure) continue;

            // Npgsql exige estrictamente DateTime con Kind=Utc para columnas 'timestamp with time zone'.
            // Normalizar explícitamente para evitar ArgumentException por Kind=Local o Unspecified
            var inicioUtc = dto.FechaHoraInicio.Kind switch
            {
                DateTimeKind.Utc => dto.FechaHoraInicio,
                DateTimeKind.Local => dto.FechaHoraInicio.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dto.FechaHoraInicio, DateTimeKind.Utc)
            };

            DateTime? finUtc = null;
            if (dto.FechaHoraFin.HasValue)
            {
                finUtc = dto.FechaHoraFin.Value.Kind switch
                {
                    DateTimeKind.Utc => dto.FechaHoraFin.Value,
                    DateTimeKind.Local => dto.FechaHoraFin.Value.ToUniversalTime(),
                    _ => DateTime.SpecifyKind(dto.FechaHoraFin.Value, DateTimeKind.Utc)
                };
            }

            // Evitar duplicar sesiones si ya se habían insertado previamente
            var yaExiste = await _context.SesionesUso
                .AnyAsync(s => s.ComputadoraId == computadora.Id 
                            && s.EmailEstudiante == emailResult.Value.Value 
                            && s.FechaHoraInicio == inicioUtc, cancellationToken);

            if (yaExiste)
            {
                procesados++;
                continue;
            }

            var sesionResult = SesionUso.Iniciar(
                computadora.Id,
                emailResult.Value,
                inicioUtc,
                SyncStatus.OfflineSync);

            if (sesionResult.IsSuccess)
            {
                var sesion = sesionResult.Value;
                if (finUtc.HasValue)
                {
                    sesion.Finalizar(dto.TipoCierre, finUtc.Value);
                }

                _context.SesionesUso.Add(sesion);
                procesados++;
            }
        }

        if (procesados > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<int>.Success(procesados);
    }
}
