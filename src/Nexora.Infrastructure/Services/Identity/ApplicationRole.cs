using Microsoft.AspNetCore.Identity;

namespace Nexora.Infrastructure.Services.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public bool IsActive { get; set; } = true;
}
