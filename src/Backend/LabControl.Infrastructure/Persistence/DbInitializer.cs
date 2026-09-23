using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LabControl.Infrastructure.Identity;

namespace LabControl.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedDatabaseAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // 1. Aplicar migraciones pendientes
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            await context.Database.MigrateAsync();
        }

        // 2. Sembrar únicamente el Rol único: Encargado
        const string encargadoRoleName = "Encargado";
        if (!await roleManager.RoleExistsAsync(encargadoRoleName))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = encargadoRoleName,
                NormalizedName = encargadoRoleName.ToUpperInvariant(),
                Descripcion = "Rol único: Encargado y Administrador del centro de cómputo"
            });
        }

        // 3. Sembrar el Período Académico inicial (Semestre Actual) si no existe
        if (!await context.PeriodosAcademicos.AnyAsync())
        {
            var periodoActualResult = LabControl.Domain.Entities.PeriodoAcademico.Create(
                nombre: "II-2026",
                fechaInicio: new DateTime(2026, 8, 1),
                fechaFin: new DateTime(2026, 12, 20),
                esActual: true
            );

            if (periodoActualResult.IsSuccess)
            {
                context.PeriodosAcademicos.Add(periodoActualResult.Value);
                await context.SaveChangesAsync();

                // Asociar bloques horarios huérfanos al período actual
                var bloquesHuerfanos = await context.BloquesHorarios
                    .Where(b => b.PeriodoAcademicoId == null)
                    .ToListAsync();

                foreach (var bloque in bloquesHuerfanos)
                {
                    bloque.AsignarPeriodo(periodoActualResult.Value.Id);
                }

                if (bloquesHuerfanos.Any())
                {
                    await context.SaveChangesAsync();
                }
            }
        }

        // 4. Sembrar Segmento de Red por defecto en Sede Sucre si no tiene
        var sedeSucre = await context.Sedes.FirstOrDefaultAsync();
        if (sedeSucre != null && string.IsNullOrWhiteSpace(sedeSucre.SegmentoRed))
        {
            sedeSucre.UpdateSegmentoRed("192.168.50.");
            await context.SaveChangesAsync();
        }
    }
}
