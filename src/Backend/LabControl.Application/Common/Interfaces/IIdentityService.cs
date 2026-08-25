namespace LabControl.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Success, string UserId, string Email, string NombreCompleto, string Role)> ValidateAdminPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
}
