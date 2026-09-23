using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Horarios.Commands.CreateBloqueHorario;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Horarios.Queries.GetHorariosByAula;

public record GetHorariosByAulaQuery(int AulaId, int? PeriodoAcademicoId = null) : IRequest<Result<List<BloqueHorarioDto>>>;

public class GetHorariosByAulaQueryHandler : IRequestHandler<GetHorariosByAulaQuery, Result<List<BloqueHorarioDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetHorariosByAulaQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BloqueHorarioDto>>> Handle(GetHorariosByAulaQuery request, CancellationToken cancellationToken)
    {
        var query = _context.BloquesHorarios
            .AsNoTracking()
            .Include(b => b.Materia)
            .Include(b => b.Docente)
            .Include(b => b.PeriodoAcademico)
            .Where(b => b.AulaId == request.AulaId);

        if (request.PeriodoAcademicoId.HasValue && request.PeriodoAcademicoId.Value > 0)
        {
            query = query.Where(b => b.PeriodoAcademicoId == request.PeriodoAcademicoId.Value);
        }
        else
        {
            var periodoActivo = await _context.PeriodosAcademicos
                .FirstOrDefaultAsync(p => p.EsActual, cancellationToken);

            if (periodoActivo != null)
            {
                query = query.Where(b => b.PeriodoAcademicoId == periodoActivo.Id || b.PeriodoAcademicoId == null);
            }
        }

        var bloques = await query
            .OrderBy(b => b.DiaSemana)
            .ThenBy(b => b.HoraInicio)
            .Select(b => new BloqueHorarioDto(
                b.Id,
                b.AulaId,
                b.DiaSemana,
                b.HoraInicio,
                b.HoraFin,
                b.EsRecreo,
                b.Descripcion,
                b.MateriaId,
                b.Materia != null ? b.Materia.Sigla : null,
                b.Materia != null ? b.Materia.Nombre : b.MateriaNombreManual,
                b.DocenteId,
                b.Docente != null ? b.Docente.NombreCompleto : b.DocenteNombreManual,
                b.GrupoParalelo,
                b.EsUsoLibre,
                b.PeriodoAcademicoId,
                b.PeriodoAcademico != null ? b.PeriodoAcademico.Nombre : null,
                b.DocenteNombreManual,
                b.DocenteEmailManual,
                b.MateriaNombreManual
            ))
            .ToListAsync(cancellationToken);

        return Result<List<BloqueHorarioDto>>.Success(bloques);
    }
}
