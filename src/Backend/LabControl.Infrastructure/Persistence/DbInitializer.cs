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

        // 2. Sembrar únicamente el Rol de Admin
        const string adminRoleName = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = adminRoleName,
                NormalizedName = adminRoleName.ToUpperInvariant(),
                Descripcion = "Rol Administrador del sistema"
            });
        }

        // 3. Sembrar únicamente el Usuario Administrador Principal
        var adminEmail = "admin@univalle.edu";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                NombreCompleto = "Administrador",
                Activo = true
            };

            var result = await userManager.CreateAsync(adminUser, "AdminPass123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRoleName);
            }
        }
    }
}
