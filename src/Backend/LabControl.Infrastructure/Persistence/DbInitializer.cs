using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LabControl.Infrastructure.Identity;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;

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

        // 4. Sembrar Aulas (A-302, A-303, A-308 con capacidad 26) y Bloques Horarios
        if (!await context.Aulas.AnyAsync())
        {
            var aula302 = Aula.Create("A-302", 26, "Bloque A - Piso 2").Value;
            var aula303 = Aula.Create("A-303", 26, "Bloque A - Piso 2").Value;
            var aula308 = Aula.Create("A-308", 26, "Bloque A - Piso 2").Value;

            context.Aulas.AddRange(aula302, aula303, aula308);
            await context.SaveChangesAsync();

            // Sembrar Bloques Horarios (Recreos y Clases de Gestión 2/2026)
            var bloques = new List<BloqueHorario>();

            // Helper para agregar recreos estándar (Lunes a Viernes)
            void AgregarRecreos(int aulaId)
            {
                var diasSemana = new[]
                {
                    DiaSemana.Lunes, DiaSemana.Martes, DiaSemana.Miercoles,
                    DiaSemana.Jueves, DiaSemana.Viernes
                };

                foreach (var dia in diasSemana)
                {
                    bloques.Add(BloqueHorario.Create(aulaId, dia, new TimeSpan(8, 40, 0), new TimeSpan(8, 50, 0), true, "Recreo Mañana 1").Value);
                    bloques.Add(BloqueHorario.Create(aulaId, dia, new TimeSpan(10, 30, 0), new TimeSpan(10, 40, 0), true, "Recreo Mañana 2").Value);
                    bloques.Add(BloqueHorario.Create(aulaId, dia, new TimeSpan(16, 30, 0), new TimeSpan(16, 40, 0), true, "Recreo Tarde").Value);
                    bloques.Add(BloqueHorario.Create(aulaId, dia, new TimeSpan(18, 20, 0), new TimeSpan(18, 30, 0), true, "Recreo Noche 1").Value);
                    bloques.Add(BloqueHorario.Create(aulaId, dia, new TimeSpan(20, 10, 0), new TimeSpan(20, 20, 0), true, "Recreo Noche 2").Value);
                }
            }

            AgregarRecreos(aula302.Id);
            AgregarRecreos(aula303.Id);
            AgregarRecreos(aula308.Id);

            // ==================== HORARIOS A-302 ====================
            // Lunes
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Lunes, new TimeSpan(7, 50, 0), new TimeSpan(9, 40, 0), false, "PROY DE SIST III (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Lunes, new TimeSpan(10, 40, 0), new TimeSpan(12, 20, 0), false, "PROG WEB I (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Lunes, new TimeSpan(15, 40, 0), new TimeSpan(18, 20, 0), false, "PROG WEB III (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Lunes, new TimeSpan(18, 30, 0), new TimeSpan(21, 10, 0), false, "SOFT QUAL ASSUR (A)").Value);
            // Martes
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Martes, new TimeSpan(7, 0, 0), new TimeSpan(9, 40, 0), false, "BASE D/DATOS I (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Martes, new TimeSpan(9, 40, 0), new TimeSpan(12, 20, 0), false, "BASE D/DATOS III (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Martes, new TimeSpan(14, 50, 0), new TimeSpan(18, 20, 0), false, "PROG WEB I (A)").Value);
            // Miercoles
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Miercoles, new TimeSpan(7, 0, 0), new TimeSpan(9, 40, 0), false, "PROG MOVIL II (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Miercoles, new TimeSpan(9, 40, 0), new TimeSpan(12, 20, 0), false, "PROGRAMACION II (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Miercoles, new TimeSpan(14, 50, 0), new TimeSpan(17, 30, 0), false, "ING SOFT (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Miercoles, new TimeSpan(18, 30, 0), new TimeSpan(21, 10, 0), false, "SOFT QUAL ASSUR (A)").Value);
            // Jueves
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Jueves, new TimeSpan(7, 50, 0), new TimeSpan(9, 40, 0), false, "RED Y COM DAT I (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Jueves, new TimeSpan(9, 40, 0), new TimeSpan(12, 20, 0), false, "PROGRAMACION II (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Jueves, new TimeSpan(15, 40, 0), new TimeSpan(18, 20, 0), false, "ESTADISTICA COM (A)").Value);
            // Viernes
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Viernes, new TimeSpan(7, 0, 0), new TimeSpan(9, 40, 0), false, "BASE D/DATOS III (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Viernes, new TimeSpan(9, 40, 0), new TimeSpan(12, 20, 0), false, "BASE D/DATOS I (A)").Value);
            bloques.Add(BloqueHorario.Create(aula302.Id, DiaSemana.Viernes, new TimeSpan(15, 40, 0), new TimeSpan(18, 20, 0), false, "PROG WEB III (A)").Value);

            // ==================== HORARIOS A-303 ====================
            // Lunes
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Lunes, new TimeSpan(7, 0, 0), new TimeSpan(8, 40, 0), false, "ADM ESTR PROY Y NEG (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Lunes, new TimeSpan(8, 50, 0), new TimeSpan(10, 30, 0), false, "JUEGO NEGOCIOS (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Lunes, new TimeSpan(10, 40, 0), new TimeSpan(12, 20, 0), false, "TEC NEGOC Y COM III (A)").Value);
            // Martes
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Martes, new TimeSpan(7, 0, 0), new TimeSpan(8, 40, 0), false, "JUEGO NEGOCIOS (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Martes, new TimeSpan(9, 40, 0), new TimeSpan(12, 20, 0), false, "ROBOTICA APLICADA (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Martes, new TimeSpan(14, 50, 0), new TimeSpan(16, 30, 0), false, "SIST INF ESTRATEG (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Martes, new TimeSpan(16, 40, 0), new TimeSpan(18, 20, 0), false, "INT MERCADOS (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Martes, new TimeSpan(18, 30, 0), new TimeSpan(20, 10, 0), false, "INV DE OPER I (A)").Value);
            // Miercoles
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Miercoles, new TimeSpan(7, 0, 0), new TimeSpan(8, 40, 0), false, "NEG INT Y ORG MULT (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Miercoles, new TimeSpan(9, 40, 0), new TimeSpan(11, 30, 0), false, "PROY DE SIST I (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Miercoles, new TimeSpan(11, 30, 0), new TimeSpan(13, 10, 0), false, "ELAB VAL INST MEDIC (A)").Value);
            // Jueves
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Jueves, new TimeSpan(7, 0, 0), new TimeSpan(8, 40, 0), false, "JUEGO NEGOCIOS (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Jueves, new TimeSpan(8, 50, 0), new TimeSpan(10, 30, 0), false, "INT MERCADOS (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Jueves, new TimeSpan(10, 40, 0), new TimeSpan(12, 20, 0), false, "MET DE INVESTIG (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Jueves, new TimeSpan(14, 50, 0), new TimeSpan(16, 30, 0), false, "TEC NEGOC Y COM III (A)").Value);
            // Viernes
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Viernes, new TimeSpan(7, 0, 0), new TimeSpan(8, 40, 0), false, "ADM ESTR PROY Y NEG (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Viernes, new TimeSpan(11, 30, 0), new TimeSpan(12, 20, 0), false, "RED COM DAT (A)").Value);
            bloques.Add(BloqueHorario.Create(aula303.Id, DiaSemana.Viernes, new TimeSpan(14, 50, 0), new TimeSpan(16, 30, 0), false, "INV OPERAC II (A)").Value);

            // ==================== HORARIOS A-308 ====================
            // Lunes
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Lunes, new TimeSpan(7, 0, 0), new TimeSpan(9, 40, 0), false, "GAME DEVELOP (A)").Value);
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Lunes, new TimeSpan(9, 40, 0), new TimeSpan(12, 20, 0), false, "REALID VIRT AUMENT (A)").Value);
            // Martes
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Martes, new TimeSpan(8, 50, 0), new TimeSpan(10, 30, 0), false, "DIB ASIST COMPUT (A)").Value);
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Martes, new TimeSpan(14, 50, 0), new TimeSpan(16, 30, 0), false, "DIS ARQ COMPUT (A)").Value);
            // Miercoles
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Miercoles, new TimeSpan(7, 0, 0), new TimeSpan(9, 40, 0), false, "REALID VIRT AUMENT (A)").Value);
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Miercoles, new TimeSpan(9, 40, 0), new TimeSpan(12, 20, 0), false, "ANIM DE PROY (A)").Value);
            // Jueves
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Jueves, new TimeSpan(7, 0, 0), new TimeSpan(9, 40, 0), false, "PROG MOVIL II (A)").Value);
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Jueves, new TimeSpan(9, 40, 0), new TimeSpan(12, 20, 0), false, "GAME DEVELOP (A)").Value);
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Jueves, new TimeSpan(14, 50, 0), new TimeSpan(17, 30, 0), false, "ANIM DE PROY (A)").Value);
            // Viernes
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Viernes, new TimeSpan(7, 0, 0), new TimeSpan(10, 30, 0), false, "TECN EMERG I (A)").Value);
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Viernes, new TimeSpan(10, 40, 0), new TimeSpan(12, 20, 0), false, "DIB ASIST COMPUT (A)").Value);
            bloques.Add(BloqueHorario.Create(aula308.Id, DiaSemana.Viernes, new TimeSpan(14, 50, 0), new TimeSpan(18, 20, 0), false, "DIS ARQ COMPUT (A)").Value);

            context.BloquesHorarios.AddRange(bloques);
            await context.SaveChangesAsync();
        }
    }
}
