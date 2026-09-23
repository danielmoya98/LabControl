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

        // 2. Sembrar únicamente el Rol único base: Encargado
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
    }
}
