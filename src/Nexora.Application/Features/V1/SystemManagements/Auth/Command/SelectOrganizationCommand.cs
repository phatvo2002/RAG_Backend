using MediatR;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Auth.Mapper;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Command;

public sealed record SelectOrganizationCommand(Guid OrganizationId)
    : ITransactionalCommand<Result<AuthResponse>>;

public sealed class SelectOrganizationCommandHandler(
    IUserContext userContext,
    IIdentityService identityService,
    IOrganizationMembershipRepository membershipRepository,
    IAuthTokenService authTokenService,
    IHttpRequestMetadata requestMetadata)
    : IRequestHandler<SelectOrganizationCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(SelectOrganizationCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<AuthResponse>.Failure(
                Error.Unauthorized(CommonMessages.Auth.LoginRequiredToSelectOrganization));
        }

        var user = await identityService.GetByIdAsync(userContext.UserId.Value, cancellationToken);
        var membership = await membershipRepository.GetForAuthenticationAsync(request.OrganizationId, userContext.UserId.Value, cancellationToken);

        if (user is null || membership is null)
        {
            return Result<AuthResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.OrganizationAccessDenied, CommonMessages.Auth.OrganizationAccessDenied));
        }

        var tokenSet = await authTokenService.IssueAsync(user, request.OrganizationId, requestMetadata.IpAddress, requestMetadata.UserAgent, cancellationToken);

        return Result<AuthResponse>.Success(AuthMapper.ToResponse(user, tokenSet));
    }
}
