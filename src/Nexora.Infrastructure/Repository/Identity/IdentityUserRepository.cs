using Microsoft.AspNetCore.Identity;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence.Repositories.Identity;

internal sealed class IdentityUserRepository(
    NexoraDbContext dbContext,
    IUserContext userContext,
    IClock clock,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
    : BaseRepository<ApplicationUser>(dbContext, userContext, clock), IUserRepository
{
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
            UserName = userName.Trim(),
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
