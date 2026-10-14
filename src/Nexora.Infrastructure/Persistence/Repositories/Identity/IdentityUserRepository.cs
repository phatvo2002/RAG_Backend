using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Helpers;
using Nexora.Application.Common.Results;
using Nexora.Domain.Organizations.Enums;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Repositories.Identity;

internal sealed class IdentityUserRepository(
    NexoraDbContext dbContext,
    IUserContext userContext,
    IClock clock,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
    : BaseRepository<ApplicationUser>(dbContext, userContext, clock), IUserRepository
{
    public async Task<BasePaginationEntity<IdentityUserSnapshot>> ListPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = DbContext.Users.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(user => user.Email).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new BasePaginationEntity<IdentityUserSnapshot>
        {
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            Data = rows.Select(ToSnapshot).ToArray()
        };
    }

    public async Task<IdentityUserSnapshot?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        (await DbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)) is { } user ? ToSnapshot(user) : null;

    public async Task<IdentityUserSnapshot?> GetActiveByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        (await DbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId && user.IsActive, cancellationToken)) is { } user ? ToSnapshot(user) : null;

    public async Task<IReadOnlyList<IdentityUserSnapshot>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        (await DbContext.Users.AsNoTracking().Where(user => userIds.Contains(user.Id)).ToListAsync(cancellationToken)).Select(ToSnapshot).ToArray();

    public async Task<IReadOnlyList<IdentityUserSnapshot>> GetByRoleAsync(Guid organizationId, Guid roleId, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
        (await (
        (from assignment in DbContext.OrganizationRoleAssignments.IgnoreQueryFilters()
         join user in DbContext.Users on assignment.UserId equals user.Id
         where assignment.OrganizationId == organizationId && assignment.RoleId == roleId && assignment.Status == OrganizationRoleAssignmentStatus.Active && assignment.ValidFrom <= utcNow && (assignment.ExpiresAt == null || assignment.ExpiresAt > utcNow) && assignment.RevokedAt == null
         orderby user.Email
         select user).AsNoTracking()).ToListAsync(cancellationToken)).Select(ToSnapshot).ToArray();

    public Task<bool> ExistsWithEmailOrUserNameAsync(Guid excludedUserId, string normalizedEmail, string normalizedUserName, CancellationToken cancellationToken) =>
        DbContext.Users.AsNoTracking().AnyAsync(user => user.Id != excludedUserId &&
            (user.NormalizedEmail == normalizedEmail || user.NormalizedUserName == normalizedUserName), cancellationToken);

    public async Task SetDetailsAsync(Guid userId, string email, string userName, string? phoneNumber, CancellationToken cancellationToken)
    {
        var user = await DbContext.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        user.Email = email;
        user.NormalizedEmail = email.ToUpperInvariant();
        user.UserName = userName;
        user.NormalizedUserName = userName.ToUpperInvariant();
        user.PhoneNumber = phoneNumber;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public async Task SetActiveAsync(Guid userId, bool active, CancellationToken cancellationToken)
    {
        var user = await DbContext.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        user.IsActive = active;
        if (active)
        {
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
        }
        else user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public async Task<IdentityUserSnapshot?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return null;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);

        return signInResult.Succeeded && user.IsActive && !IsLockedOut(user)
            ? ToSnapshot(user)
            : null;
    }

    public async Task<Result<IdentityUserSnapshot>> CreateWithPasswordAsync(
        string email,
        string userName,
        string password,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            UserName = UserHelper.NormalizeUserName(userName),
            PhoneNumber = phoneNumber?.Trim(),
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded
            ? Result<IdentityUserSnapshot>.Success(ToSnapshot(user))
            : Result<IdentityUserSnapshot>.Failure(ToError(result));
    }

    public async Task<Result> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    private static bool IsLockedOut(ApplicationUser user) =>
        user.LockoutEnabled &&
        user.LockoutEnd.HasValue &&
        user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    private static IdentityUserSnapshot ToSnapshot(ApplicationUser user) =>
        new(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            user.UserName ?? user.Email ?? string.Empty,
            user.PhoneNumber,
            user.IsActive);

    private static Error ToError(IdentityResult result)
    {
        var duplicate = result.Errors.FirstOrDefault(error => error.Code is
            CommonMessages.IdentityCodes.DuplicateUserName or
            CommonMessages.IdentityCodes.DuplicateEmail or
            CommonMessages.IdentityCodes.DuplicateRoleName);

        return duplicate is not null
            ? Error.Conflict(duplicate.Description)
            : result.Errors
                .Select(error => new Error(
                    CommonMessages.ErrorCodes.IdentityOperationFailed,
                    error.Description))
                .FirstOrDefault()
              ?? new Error(
                  CommonMessages.ErrorCodes.IdentityOperationFailed,
                  CommonMessages.Infrastructure.IdentityOperationFailed);
    }
}
