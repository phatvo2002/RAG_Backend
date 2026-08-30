using Nexora.Application.Common.Results;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Infrastructure.Identity;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IUserRepository : IBaseRepository<ApplicationUser>
{
    Task<List<ApplicationUser>> GetByRoleAsync(Guid organizationId, Guid roleId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<IdentityUserSnapshot?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    Task<Result<IdentityUserSnapshot>> CreateWithPasswordAsync(
        string email,
        string userName,
        string password,
        string? phoneNumber,
        CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);
}
