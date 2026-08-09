using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Authentication;

internal sealed class IdentityService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, SignInManager<ApplicationUser> signInManager)
    : IIdentityService
{
    public async Task<IdentityUserSnapshot?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return null;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        return signInResult.Succeeded && user.IsActive && !IsLockedOut(user)
            ? ToSnapshot(user)
            : null;
    }

    public async Task<IdentityUserSnapshot?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && user.IsActive && !IsLockedOut(user)
            ? ToSnapshot(user)
            : null;
    }

    public async Task<IdentityUserSnapshot?> GetUserForManagementAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToSnapshot(user);
    }

    public async Task<PagedResult<IdentityUserSnapshot>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var query = userManager.Users.AsNoTracking().OrderBy(user => user.Email);
        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToListAsync(cancellationToken);
        return new PagedResult<IdentityUserSnapshot>(users.Select(ToSnapshot).ToArray(), normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<Result<IdentityUserSnapshot>> CreateUserAsync(string email, string userName, string password, string? phoneNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email.Trim(), UserName = userName.Trim(), PhoneNumber = phoneNumber?.Trim(), IsActive = true };
        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded ? Result<IdentityUserSnapshot>.Success(ToSnapshot(user)) : Result<IdentityUserSnapshot>.Failure(ToError(result));
    }

    public async Task<Result<IdentityUserSnapshot>> UpdateUserAsync(Guid userId, string email, string userName, string? phoneNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<IdentityUserSnapshot>.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        user.Email = email.Trim();
        user.UserName = userName.Trim();
        user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
        user.NormalizedUserName = userManager.NormalizeName(user.UserName);
        user.PhoneNumber = phoneNumber?.Trim();
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? Result<IdentityUserSnapshot>.Success(ToSnapshot(user)) : Result<IdentityUserSnapshot>.Failure(ToError(result));
    }

    public async Task<Result> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result> ReactivateUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.NotFound));
        }

        user.IsActive = true;
        user.LockoutEnd = null;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
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

    public async Task<PagedResult<IdentityRoleSnapshot>> GetRolesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var query = roleManager.Roles.AsNoTracking().OrderBy(role => role.Name);
        var totalCount = await query.CountAsync(cancellationToken);
        var roles = await query.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToListAsync(cancellationToken);
        return new PagedResult<IdentityRoleSnapshot>(roles.Select(ToSnapshot).ToArray(), normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<IdentityRoleSnapshot?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        return role is null ? null : ToSnapshot(role);
    }

    public async Task<Result<IdentityRoleSnapshot>> CreateRoleAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = new ApplicationRole { Id = Guid.NewGuid(), Name = name.Trim(), IsActive = true };
        var result = await roleManager.CreateAsync(role);
        return result.Succeeded ? Result<IdentityRoleSnapshot>.Success(ToSnapshot(role)) : Result<IdentityRoleSnapshot>.Failure(ToError(result));
    }

    public async Task<Result<IdentityRoleSnapshot>> UpdateRoleAsync(Guid roleId, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return Result<IdentityRoleSnapshot>.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        role.Name = name.Trim();
        role.NormalizedName = roleManager.NormalizeKey(role.Name);
        var result = await roleManager.UpdateAsync(role);
        return result.Succeeded ? Result<IdentityRoleSnapshot>.Success(ToSnapshot(role)) : Result<IdentityRoleSnapshot>.Failure(ToError(result));
    }

    public async Task<Result> DeactivateRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        role.IsActive = false;
        var result = await roleManager.UpdateAsync(role);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result> ReactivateRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        role.IsActive = true;
        var result = await roleManager.UpdateAsync(role);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    private static bool IsLockedOut(ApplicationUser user) =>
        user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    private static IdentityUserSnapshot ToSnapshot(ApplicationUser user) => new(user.Id, user.Email ?? user.UserName ?? string.Empty, user.UserName ?? user.Email ?? string.Empty, user.PhoneNumber, user.IsActive);

    private static IdentityRoleSnapshot ToSnapshot(ApplicationRole role) => new(role.Id, role.Name ?? string.Empty, role.IsActive);

    private static Error ToError(IdentityResult result)
    {
        var duplicate = result.Errors.FirstOrDefault(error => error.Code is CommonMessages.IdentityCodes.DuplicateUserName or CommonMessages.IdentityCodes.DuplicateEmail or CommonMessages.IdentityCodes.DuplicateRoleName);
        return duplicate is not null ? Error.Conflict(duplicate.Description) : result.Errors.Select(error => new Error(CommonMessages.ErrorCodes.IdentityOperationFailed, error.Description)).FirstOrDefault() ?? new Error(CommonMessages.ErrorCodes.IdentityOperationFailed, CommonMessages.Infrastructure.IdentityOperationFailed);
    }
}
