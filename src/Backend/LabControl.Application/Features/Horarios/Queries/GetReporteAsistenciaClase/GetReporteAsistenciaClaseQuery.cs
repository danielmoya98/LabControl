using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Horarios.Queries.GetReporteAsistenciaClase;

public record GetReporteAsistenciaClaseQuery(int BloqueHorarioId, DateTime? Fecha = null) 
    : IRequest<Result<ReporteAsistenciaClaseDto>>;

public class GetReporteAsistenciaClaseQueryHandler 
    : IRequestHandler<GetReporteAsistenciaClaseQuery, Result<ReporteAsistenciaClaseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetReporteAsistenciaClaseQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ReporteAsistenciaClaseDto>> Handle(GetReporteAsistenciaClaseQuery request, CancellationToken cancellationToken)
    {
        var bloque = await _context.BloquesHorarios
            .Include(b => b.Aula)
            .Include(b => b.Materia)
            .Include(b => b.Docente)
            .FirstOrDefaultAsync(b => b.Id == request.BloqueHorarioId, cancellationToken);

        if (bloque == null)
        {
            return Result<ReporteAsistenciaClaseDto>.Failure(
                Error.NotFound("BloqueHorario.NotFound", $"No se encontró el bloque horario con ID {request.BloqueHorarioId}."));
        }

        var fechaClase = request.Fecha?.Date ?? DateTime.UtcNow.Date;
        var sede = await _context.Sedes.FirstOrDefaultAsync(cancellationToken);
        string sedeNombre = sede?.Nombre ?? "Universidad del Valle - Sede Sucre";

        // Ventana de tiempo con margen de 15 minutos de anticipación para estudiantes puntuales
        var fechaInicioUtc = DateTime.SpecifyKind(fechaClase.Add(bloque.HoraInicio).AddMinutes(-15), DateTimeKind.Utc);
        var fechaFinUtc = DateTime.SpecifyKind(fechaClase.Add(bloque.HoraFin).AddMinutes(15), DateTimeKind.Utc);

        // Obtener computadoras del aula para mapear puestos
        var computadorasAula = await _context.Computadoras
            .Where(c => c.AulaId == bloque.AulaId)
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        // Obtener sesiones registradas en el aula durante el período de la clase
        var sesiones = await _context.SesionesUso
            .Include(s => s.Computadora)
            .Where(s => s.Computadora.AulaId == bloque.AulaId &&
                        s.FechaHoraInicio >= fechaInicioUtc &&
                        s.FechaHoraInicio <= fechaFinUtc)
            .OrderBy(s => s.Computadora.NumeroPuesto)
            .ThenBy(s => s.Computadora.Hostname)
            .ToListAsync(cancellationToken);

        var estudiantesList = sesiones.Select(s =>
        {
            int puesto = s.Computadora?.NumeroPuesto ?? 0;
            string hostname = s.Computadora?.Hostname ?? $"PC-{s.ComputadoraId}";
            string tipoCierre = s.TipoCierre.ToString();

            return new AsistenciaEstudianteItemDto(
                puesto,
                hostname,
                s.EmailEstudiante,
                s.FechaHoraInicio,
                s.FechaHoraFin,
                s.DuracionMinutos,
                tipoCierre
            );
        }).ToList();

        var dto = new ReporteAsistenciaClaseDto(
            sedeNombre,
            bloque.Aula?.Nombre ?? $"Aula #{bloque.AulaId}",
            bloque.Materia?.Nombre ?? (!string.IsNullOrWhiteSpace(bloque.MateriaNombreManual) ? bloque.MateriaNombreManual : (bloque.Descripcion ?? "Clase Programada")),
            bloque.Materia?.Sigla,
            bloque.Docente != null ? $"{bloque.Docente.Nombres} {bloque.Docente.Apellidos}" : (!string.IsNullOrWhiteSpace(bloque.DocenteNombreManual) ? bloque.DocenteNombreManual : null),
            bloque.Docente?.EmailInstitucional ?? bloque.DocenteEmailManual,
            bloque.GrupoParalelo,
            fechaClase,
            bloque.HoraInicio,
            bloque.HoraFin,
            bloque.Aula?.Capacidad ?? computadorasAula.Count,
            estudiantesList
        );

        return Result<ReporteAsistenciaClaseDto>.Success(dto);
    }
}
