using Microsoft.AspNetCore.Identity;

namespace LabControl.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public string? Descripcion { get; set; }
}
