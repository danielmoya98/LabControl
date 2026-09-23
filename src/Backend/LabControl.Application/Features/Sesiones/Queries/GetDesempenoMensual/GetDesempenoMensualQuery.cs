using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Sesiones.Queries.GetDesempenoMensual;

public record MesDesempenoDto(
    string MesLabel,
    int Anio,
    int Mes,
    int SesionesActivas,
    int SesionesCompletadas,
    int InfraccionesEnergia
);

public record DesempenoMensualDto(
    List<MesDesempenoDto> Meses
);

public record GetDesempenoMensualQuery(int Meses = 12) : IRequest<Result<DesempenoMensualDto>>;

public class GetDesempenoMensualQueryHandler : IRequestHandler<GetDesempenoMensualQuery, Result<DesempenoMensualDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDesempenoMensualQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DesempenoMensualDto>> Handle(GetDesempenoMensualQuery request, CancellationToken cancellationToken)
    {
        int totalMeses = Math.Clamp(request.Meses, 1, 36);
        var ahoraUtc = DateTime.UtcNow;
        var primerDiaMesActual = new DateTime(ahoraUtc.Year, ahoraUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaInicioRango = primerDiaMesActual.AddMonths(-totalMeses + 1);

        // Consultar sesiones en el rango
        var sesionesEnRango = await _context.SesionesUso
            .AsNoTracking()
            .Where(s => s.FechaHoraInicio >= fechaInicioRango)
            .Select(s => new
            {
                s.FechaHoraInicio.Year,
                s.FechaHoraInicio.Month,
                EsActiva = s.FechaHoraFin == null
            })
            .ToListAsync(cancellationToken);

        // Consultar incidentes de energía en el rango
        var incidentesEnRango = await _context.RegistrosConsumoEnergia
            .AsNoTracking()
            .Where(r => r.FechaDeteccionUtc >= fechaInicioRango)
            .Select(r => new
            {
                r.FechaDeteccionUtc.Year,
                r.FechaDeteccionUtc.Month
            })
            .ToListAsync(cancellationToken);

        var cultura = new CultureInfo("es-ES");
        var resultadoMeses = new List<MesDesempenoDto>();

        for (int i = 0; i < totalMeses; i++)
        {
            var fechaIteracion = fechaInicioRango.AddMonths(i);
            int anio = fechaIteracion.Year;
            int mes = fechaIteracion.Month;

            int activas = sesionesEnRango.Count(s => s.Year == anio && s.Month == mes && s.EsActiva);
            int completadas = sesionesEnRango.Count(s => s.Year == anio && s.Month == mes && !s.EsActiva);
            int incidentes = incidentesEnRango.Count(r => r.Year == anio && r.Month == mes);

            string label = fechaIteracion.ToString("MMM", cultura).ToUpperInvariant();

            resultadoMeses.Add(new MesDesempenoDto(label, anio, mes, activas, completadas, incidentes));
        }

        return Result<DesempenoMensualDto>.Success(new DesempenoMensualDto(resultadoMeses));
    }
}
