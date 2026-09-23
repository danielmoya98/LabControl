using Microsoft.EntityFrameworkCore;
using LabControl.Domain.Entities;

namespace LabControl.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Sede> Sedes { get; }
    DbSet<Bloque> Bloques { get; }
    DbSet<Aula> Aulas { get; }
    DbSet<Computadora> Computadoras { get; }
    DbSet<BloqueHorario> BloquesHorarios { get; }
    DbSet<Docente> Docentes { get; }
    DbSet<Materia> Materias { get; }
    DbSet<SesionUso> SesionesUso { get; }
    DbSet<RegistroConsumoEnergia> RegistrosConsumoEnergia { get; }
    DbSet<PeriodoAcademico> PeriodosAcademicos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
