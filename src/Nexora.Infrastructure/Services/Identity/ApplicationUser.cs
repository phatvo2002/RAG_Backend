using Microsoft.AspNetCore.Identity;

namespace Nexora.Infrastructure.Services.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}
