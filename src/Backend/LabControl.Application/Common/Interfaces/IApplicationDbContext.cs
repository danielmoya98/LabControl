using Microsoft.EntityFrameworkCore;
using LabControl.Domain.Entities;

namespace LabControl.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Aula> Aulas { get; }
    DbSet<Computadora> Computadoras { get; }
    DbSet<BloqueHorario> BloquesHorarios { get; }
    DbSet<SesionUso> SesionesUso { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
