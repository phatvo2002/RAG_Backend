using Microsoft.AspNetCore.Identity;

namespace Nexora.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    protected ApplicationRole()
    {
    }
}
