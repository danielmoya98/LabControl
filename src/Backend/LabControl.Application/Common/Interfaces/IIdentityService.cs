namespace LabControl.Application.Common.Interfaces;

public record UserDto(
    string Id,
    string Email,
    string NombreCompleto,
    string Role,
    bool Activo,
    DateTime FechaRegistroUtc
);

public interface IIdentityService
{
    Task<(bool Success, string UserId, string Email, string NombreCompleto, string Role)> ValidateAdminPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<(bool Success, string UserId, string[] Errors)> CreateUserAsync(string email, string password, string nombreCompleto, string role = "Encargado");
    Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string[] Errors)> UpdateUserAsync(string userId, string nombreCompleto, string role, bool activo, string? newPassword = null);
    Task<(bool Success, string[] Errors)> DeleteUserAsync(string userId);
}
