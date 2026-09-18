using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Energia.Commands.EvaluarEquiposEncendidos;

public record EvaluarEquiposEncendidosCommand : IRequest<Result<int>>;

public class EvaluarEquiposEncendidosCommandHandler : IRequestHandler<EvaluarEquiposEncendidosCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;

    public EvaluarEquiposEncendidosCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(EvaluarEquiposEncendidosCommand request, CancellationToken cancellationToken)
    {
        // Computadoras con heartbeat reciente (encendidas) y sin sesión activa
        var haceCincoMinutos = DateTime.UtcNow.AddMinutes(-5);
        var computadorasEncendidas = await _context.Computadoras
            .Include(c => c.SesionesUso)
            .Where(c => c.UltimoHeartbeatUtc >= haceCincoMinutos && c.EstadoActual == EstadoComputadora.Disponible)
            .ToListAsync(cancellationToken);

        int nuevosIncidentes = 0;
        var haceCuatroHoras = DateTime.UtcNow.AddHours(-4);

        foreach (var pc in computadorasEncendidas)
        {
            // Solo si tiene registro de un último estudiante que la utilizó
            if (string.IsNullOrWhiteSpace(pc.UltimoEstudianteEmail)) continue;

            // Verificar si ya fue registrado un incidente para esta PC recientemente
            var yaRegistrado = await _context.RegistrosConsumoEnergia
                .AnyAsync(r => r.ComputadoraId == pc.Id && r.FechaDeteccionUtc >= haceCuatroHoras, cancellationToken);

            if (yaRegistrado) continue;

            var uptime = pc.UptimeHoras ?? 1.0;
            var motivo = uptime >= 8.0
                ? "Equipo encendido toda la noche / fuera de horario laboral"
                : "Equipo desatendido y no apagado tras finalizar el uso";

            var registroResult = RegistroConsumoEnergia.Create(
                pc.Id,
                pc.AulaId,
                pc.UltimoEstudianteEmail,
                pc.UltimoEstudianteNombre,
                uptime,
                motivo
            );

            if (registroResult.IsSuccess)
            {
                _context.RegistrosConsumoEnergia.Add(registroResult.Value);
                nuevosIncidentes++;
            }
        }

        if (nuevosIncidentes > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<int>.Success(nuevosIncidentes);
    }
}
