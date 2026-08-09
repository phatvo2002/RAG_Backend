using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Abstractions.Auth;

public interface IIdentityService
{
    Task<IdentityUserSnapshot?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<IdentityUserSnapshot?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IdentityUserSnapshot?> GetUserForManagementAsync(Guid userId, CancellationToken cancellationToken);

    Task<PagedResult<IdentityUserSnapshot>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Result<IdentityUserSnapshot>> CreateUserAsync(string email, string userName, string password, string? phoneNumber, CancellationToken cancellationToken);

    Task<Result<IdentityUserSnapshot>> UpdateUserAsync(Guid userId, string email, string userName, string? phoneNumber, CancellationToken cancellationToken);

    Task<Result> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result> ReactivateUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);

    Task<PagedResult<IdentityRoleSnapshot>> GetRolesAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<IdentityRoleSnapshot?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken);

    Task<Result<IdentityRoleSnapshot>> CreateRoleAsync(string name, CancellationToken cancellationToken);

    Task<Result<IdentityRoleSnapshot>> UpdateRoleAsync(Guid roleId, string name, CancellationToken cancellationToken);

    Task<Result> DeactivateRoleAsync(Guid roleId, CancellationToken cancellationToken);

    Task<Result> ReactivateRoleAsync(Guid roleId, CancellationToken cancellationToken);
}

public sealed record IdentityUserSnapshot(Guid UserId, string Email, string UserName, string? PhoneNumber = null, bool IsActive = true);

public sealed record IdentityRoleSnapshot(Guid RoleId, string Name, bool IsActive);
