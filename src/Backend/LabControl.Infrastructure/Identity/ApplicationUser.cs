using Microsoft.AspNetCore.Identity;

namespace LabControl.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = default!;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistroUtc { get; set; } = DateTime.UtcNow;
}
