using Microsoft.AspNetCore.Identity;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Authentication;

internal sealed class IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
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

        return signInResult.Succeeded && !IsLockedOut(user)
            ? ToSnapshot(user)
            : null;
    }

    public async Task<IdentityUserSnapshot?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && !IsLockedOut(user)
            ? ToSnapshot(user)
            : null;
    }

    private static bool IsLockedOut(ApplicationUser user) =>
        user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    private static IdentityUserSnapshot ToSnapshot(ApplicationUser user) =>
        new(user.Id, user.Email ?? user.UserName ?? string.Empty, user.UserName ?? user.Email ?? string.Empty);
}
