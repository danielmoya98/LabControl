using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Entities;
using LabControl.Infrastructure.Identity;

namespace LabControl.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<Bloque> Bloques => Set<Bloque>();
    public DbSet<Aula> Aulas => Set<Aula>();
    public DbSet<Computadora> Computadoras => Set<Computadora>();
    public DbSet<BloqueHorario> BloquesHorarios => Set<BloqueHorario>();
    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<SesionUso> SesionesUso => Set<SesionUso>();
    public DbSet<RegistroConsumoEnergia> RegistrosConsumoEnergia => Set<RegistroConsumoEnergia>();
    public DbSet<PeriodoAcademico> PeriodosAcademicos => Set<PeriodoAcademico>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
