using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Common.Interfaces;

namespace LabControl.Api.Controllers;

public record CreateUsuarioRequest(string Email, string Password, string NombreCompleto, string Role);
public record UpdateUsuarioRequest(string NombreCompleto, string Role, bool Activo, string? NewPassword = null);

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public UsuariosController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsuarios(CancellationToken cancellationToken)
    {
        var usuarios = await _identityService.GetUsersAsync(cancellationToken);
        return Ok(usuarios);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUsuario([FromBody] CreateUsuarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.NombreCompleto))
        {
            return BadRequest(new { error = "El correo, la contraseña y el nombre completo son obligatorios." });
        }

        var (success, userId, errors) = await _identityService.CreateUserAsync(
            request.Email,
            request.Password,
            request.NombreCompleto,
            string.IsNullOrWhiteSpace(request.Role) ? "Encargado" : request.Role.Trim()
        );

        if (!success)
        {
            return BadRequest(new { errors });
        }

        return Ok(new { ok = true, userId, mensaje = "Usuario creado exitosamente." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUsuario(string id, [FromBody] UpdateUsuarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "ID de usuario inválido." });
        }

        var (success, errors) = await _identityService.UpdateUserAsync(
            id,
            request.NombreCompleto,
            request.Role,
            request.Activo,
            request.NewPassword
        );

        if (!success)
        {
            return BadRequest(new { errors });
        }

        return Ok(new { ok = true, mensaje = "Usuario actualizado exitosamente." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUsuario(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "ID de usuario inválido." });
        }

        var (success, errors) = await _identityService.DeleteUserAsync(id);
        if (!success)
        {
            return BadRequest(new { errors });
        }

        return Ok(new { ok = true, mensaje = "Usuario eliminado exitosamente." });
    }
}
