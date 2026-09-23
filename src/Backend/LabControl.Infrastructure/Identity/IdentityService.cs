using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;

namespace LabControl.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<(bool Success, string UserId, string Email, string NombreCompleto, string Role)> ValidateAdminPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.Activo)
        {
            return (false, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        bool isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            return (false, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        var roles = await _userManager.GetRolesAsync(user);
        string userRole = roles.FirstOrDefault() ?? "Encargado";

        return (true, user.Id, user.Email!, user.NombreCompleto, userRole);
    }

    public async Task<(bool Success, string UserId, string[] Errors)> CreateUserAsync(string email, string password, string nombreCompleto, string role = "Encargado")
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser != null)
        {
            return (false, string.Empty, ["Ya existe un usuario registrado con este correo institucional."]);
        }

        // Asegurar que el rol existe
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new ApplicationRole
            {
                Name = role,
                NormalizedName = role.ToUpperInvariant(),
                Descripcion = "Rol del sistema"
            });
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            NombreCompleto = nombreCompleto.Trim(),
            Activo = true,
            FechaRegistroUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, string.Empty, result.Errors.Select(e => e.Description).ToArray());
        }

        await _userManager.AddToRoleAsync(user, role);

        return (true, user.Id, []);
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(cancellationToken);

        var list = new List<UserDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            string role = roles.FirstOrDefault() ?? "Encargado";
            list.Add(new UserDto(
                u.Id,
                u.Email ?? u.UserName ?? "",
                u.NombreCompleto,
                role,
                u.Activo,
                u.FechaRegistroUtc
            ));
        }

        return list;
    }

    public async Task<(bool Success, string[] Errors)> UpdateUserAsync(string userId, string nombreCompleto, string role, bool activo, string? newPassword = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, ["Usuario no encontrado."]);
        }

        user.NombreCompleto = nombreCompleto.Trim();
        user.Activo = activo;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, updateResult.Errors.Select(e => e.Description).ToArray());
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole
                {
                    Name = role,
                    NormalizedName = role.ToUpperInvariant(),
                    Descripcion = "Rol del sistema"
                });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!passResult.Succeeded)
            {
                return (false, passResult.Errors.Select(e => e.Description).ToArray());
            }
        }

        return (true, []);
    }

    public async Task<(bool Success, string[] Errors)> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, ["Usuario no encontrado."]);
        }

        if (user.Email?.Equals("admin@univalle.edu", StringComparison.OrdinalIgnoreCase) == true)
        {
            return (false, ["No es posible eliminar el usuario administrador principal del sistema."]);
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description).ToArray());
        }

        return (true, []);
    }
}
