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

public sealed record LoginCommand(string Email, string Password, Guid OrganizationId) : ITransactionalCommand<Result<AuthResponse>>;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    IOrganizationMembershipRepository membershipRepository,
    IAuthTokenService authTokenService,
    IHttpRequestMetadata requestMetadata)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        if (user is null)
        {
            return Result<AuthResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.InvalidCredentials, CommonMessages.Auth.InvalidCredentials));
        }

        var membership = await membershipRepository.GetForAuthenticationAsync(request.OrganizationId, user.UserId, cancellationToken);

        if (membership is null)
        {
            return Result<AuthResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.OrganizationAccessDenied, CommonMessages.Auth.OrganizationAccessDenied));
        }

        var tokenSet = await authTokenService.IssueAsync(user, request.OrganizationId, requestMetadata.IpAddress, requestMetadata.UserAgent, cancellationToken);

        return Result<AuthResponse>.Success(AuthMapper.ToResponse(user, tokenSet));
    }
}
