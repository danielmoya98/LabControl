using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Enums;

namespace LabControl.Application.Features.Sesiones.Queries.GetSesionesAuditoria;

public record GetSesionesAuditoriaQuery(
    DateTime? FechaInicio = null,
    DateTime? FechaFin = null,
    int? AulaId = null,
    string? EmailEstudiante = null,
    string? Hostname = null,
    TipoCierreSesion? TipoCierre = null,
    int Pagina = 1,
    int TamanioPagina = 50
) : IRequest<Result<ResultadoAuditoriaDto>>;

public record ResultadoAuditoriaDto(
    List<SesionAuditoriaDto> Sesiones,
    int TotalRegistros,
    int PaginaActual,
    int TotalPaginas,
    int TotalMinutosUso,
    int TotalEstudiantesUnicos
);

public class GetSesionesAuditoriaQueryHandler : IRequestHandler<GetSesionesAuditoriaQuery, Result<ResultadoAuditoriaDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSesionesAuditoriaQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ResultadoAuditoriaDto>> Handle(GetSesionesAuditoriaQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SesionesUso
            .Include(s => s.Computadora)
            .ThenInclude(c => c.Aula)
            .AsNoTracking();

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(s => s.FechaHoraInicio >= request.FechaInicio.Value);
        }

        if (request.FechaFin.HasValue)
        {
            query = query.Where(s => s.FechaHoraInicio <= request.FechaFin.Value);
        }

        if (request.AulaId.HasValue && request.AulaId.Value > 0)
        {
            query = query.Where(s => s.Computadora.AulaId == request.AulaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EmailEstudiante))
        {
            var emailNorm = request.EmailEstudiante.Trim().ToLower();
            query = query.Where(s => s.EmailEstudiante.ToLower().Contains(emailNorm));
        }

        if (!string.IsNullOrWhiteSpace(request.Hostname))
        {
            var hostNorm = request.Hostname.Trim().ToUpper();
            query = query.Where(s => s.Computadora.Hostname.ToUpper().Contains(hostNorm));
        }

        if (request.TipoCierre.HasValue)
        {
            query = query.Where(s => s.TipoCierre == request.TipoCierre.Value);
        }

        // Totales y KPIs
        var totalRegistros = await query.CountAsync(cancellationToken);
        var totalMinutos = totalRegistros > 0 
            ? await query.SumAsync(s => s.DuracionMinutos ?? 0, cancellationToken) 
            : 0;
        var totalEstudiantesUnicos = totalRegistros > 0 
            ? await query.Select(s => s.EmailEstudiante).Distinct().CountAsync(cancellationToken) 
            : 0;

        var pagina = request.Pagina < 1 ? 1 : request.Pagina;
        var tamanio = request.TamanioPagina < 1 ? 50 : request.TamanioPagina;
        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanio);

        var sesiones = await query
            .OrderByDescending(s => s.FechaHoraInicio)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .Select(s => new SesionAuditoriaDto(
                s.Id,
                s.ComputadoraId,
                s.Computadora.Hostname,
                s.Computadora.AulaId,
                s.Computadora.Aula.Nombre,
                s.EmailEstudiante,
                s.FechaHoraInicio,
                s.FechaHoraFin,
                s.DuracionMinutos,
                s.TipoCierre,
                s.SyncStatus,
                s.FechaSincronizacion
            ))
            .ToListAsync(cancellationToken);

        var resultado = new ResultadoAuditoriaDto(
            sesiones,
            totalRegistros,
            pagina,
            totalPaginas,
            totalMinutos,
            totalEstudiantesUnicos
        );

        return Result<ResultadoAuditoriaDto>.Success(resultado);
    }
}
