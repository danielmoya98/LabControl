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

    public SincronizarSesionesBatchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(SincronizarSesionesBatchCommand request, CancellationToken cancellationToken)
    {
        var computadora = await _context.Computadoras
            .FirstOrDefaultAsync(c => c.Hostname == request.Hostname.ToUpperInvariant(), cancellationToken);

        if (computadora == null)
        {
            return Result<int>.Failure(Error.NotFound("Computadora.NotFound", $"La computadora {request.Hostname} no existe."));
        }

        int procesados = 0;

        foreach (var dto in request.Sesiones)
        {
            var emailResult = EmailInstitucional.Create(dto.EmailEstudiante);
            if (emailResult.IsFailure) continue;

            var sesionResult = SesionUso.Iniciar(
                computadora.Id,
                emailResult.Value,
                dto.FechaHoraInicio,
                SyncStatus.OfflineSync);

            if (sesionResult.IsSuccess)
            {
                var sesion = sesionResult.Value;
                if (dto.FechaHoraFin.HasValue)
                {
                    sesion.Finalizar(dto.TipoCierre, dto.FechaHoraFin.Value);
                }

                _context.SesionesUso.Add(sesion);
                procesados++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(procesados);
    }
}
