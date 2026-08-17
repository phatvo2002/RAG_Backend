using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;
using DomainApiFunction = Nexora.Domain.Authorization.ApiFunction;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Mapper;

internal static class AuthMapper
{
    public static AuthResponse ToResponse(IdentityUserSnapshot user, AuthTokenSet tokenSet, IEnumerable<DomainApiFunction> apiFunctions) =>
        new(
            user.UserId,
            user.Email,
            "Bearer",
            tokenSet.AccessToken,
            tokenSet.AccessTokenExpiresAt,
            tokenSet.RefreshToken,
            tokenSet.RefreshTokenExpiresAt,
            apiFunctions.Select(ApiFunctionResponse.FromEntity).ToArray());
}
