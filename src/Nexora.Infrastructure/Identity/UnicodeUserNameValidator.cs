using Microsoft.AspNetCore.Identity;
using Nexora.Application.Common.Helpers;

namespace Nexora.Infrastructure.Identity;

internal sealed class UnicodeUserNameValidator : IUserValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        var userName = user.UserName;
        return Task.FromResult(UserHelper.IsValidUserName(userName)
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidUserNameCharacters",
                Description = UserHelper.InvalidUserNameMessage
            }));
    }
}
