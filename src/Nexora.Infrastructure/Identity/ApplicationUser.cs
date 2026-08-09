using Microsoft.AspNetCore.Identity;

namespace Nexora.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    protected ApplicationUser()
    {
    }
}
