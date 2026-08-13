using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Mapper;

internal static class AuthMapper
{
    public static AuthResponse ToResponse(IdentityUserSnapshot user, AuthTokenSet tokenSet) =>
        new(
            user.UserId,
            user.Email,
            "Bearer",
            tokenSet.AccessToken,
            tokenSet.AccessTokenExpiresAt,
            tokenSet.RefreshToken,
            tokenSet.RefreshTokenExpiresAt);
}
