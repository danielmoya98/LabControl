using Microsoft.AspNetCore.Identity;
using LabControl.Application.Common.Interfaces;

namespace LabControl.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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
        string userRole = roles.FirstOrDefault() ?? "EncargadoLaboratorio";

        return (true, user.Id, user.Email!, user.NombreCompleto, userRole);
    }
}
