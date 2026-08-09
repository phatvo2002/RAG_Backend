using Microsoft.AspNetCore.Identity;

namespace Nexora.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public bool IsActive { get; set; } = true;

    public ApplicationRole()
    {
    }
}
