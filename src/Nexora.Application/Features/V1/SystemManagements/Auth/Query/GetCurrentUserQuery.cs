using MediatR;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Query;

public sealed record GetCurrentUserQuery : IQuery<Result<CurrentUserResponse>>;

public sealed class GetCurrentUserQueryHandler(
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IDefaultOrganizationProvider organizationProvider,
    IClock clock)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<CurrentUserResponse>.Failure(
                Error.Unauthorized(CommonMessages.Auth.LoginRequiredToViewCurrentUser));
        }

        var user = await unitOfWork.UserRepository.GetActiveByIdAsync(userContext.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result<CurrentUserResponse>.Failure(
                Error.Unauthorized(CommonMessages.Auth.UserNotValid));
        }

        var profile = await unitOfWork.UserProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var organizationId = tenantContext.OrganizationId
            ?? await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var apiFunctions = organizationId.HasValue
            ? await unitOfWork.ApiFunctionRepository.GetEffectiveByUserAsync(
                organizationId.Value,
                user.Id,
                clock.UtcNow,
                cancellationToken)
            : [];
        var menuParentCodes = apiFunctions
            .Where(function => function.IsMenuVisible && !string.IsNullOrWhiteSpace(function.MenuParentCode))
            .Select(function => function.MenuParentCode!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var menuParents = (await unitOfWork.ApiFunctionRepository.GetActiveMenuParentsAsync(cancellationToken))
            .Where(function => menuParentCodes.Contains(function.Code.Value))
            .ToArray();
        var menuFunctions = menuParents
            .Concat(apiFunctions)
            .ToArray();

        return Result<CurrentUserResponse>.Success(
            new CurrentUserResponse(
                user.Id,
                user.Email ?? user.UserName ?? string.Empty,
                user.UserName ?? user.Email ?? string.Empty,
                profile?.DisplayName,
                ApiFunctionMenuTreeBuilder.Build(menuFunctions)));
    }
}
