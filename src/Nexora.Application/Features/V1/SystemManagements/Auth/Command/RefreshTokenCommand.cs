using MediatR;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Auth.Mapper;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Command;

public sealed record RefreshTokenCommand(string RefreshToken) : ITransactionalCommand<Result<AuthResponse>>;

public sealed class RefreshTokenCommandHandler(IAuthTokenService authTokenService, IUnitOfWork unitOfWork, IHttpRequestMetadata requestMetadata, IClock clock) : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenSet = await authTokenService.RotateRefreshTokenAsync(request.RefreshToken, requestMetadata.IpAddress, requestMetadata.UserAgent, cancellationToken);

        if (tokenSet is null)
        {
            return Result<AuthResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.InvalidRefreshToken, CommonMessages.Auth.InvalidRefreshToken));
        }

        var userEntity = await unitOfWork.UserRepository.GetAsync(
            user => user.Id == tokenSet.UserId && user.IsActive,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        var user = userEntity is null
            ? null
            : new IdentityUserSnapshot(
                userEntity.Id,
                userEntity.Email ?? userEntity.UserName ?? string.Empty,
                userEntity.UserName ?? userEntity.Email ?? string.Empty,
                userEntity.PhoneNumber,
                userEntity.IsActive);
        if (user is null)
        {
            return Result<AuthResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.UserNotFound, CommonMessages.Auth.UserNotFound));
        }

        var apiFunctions = await unitOfWork.ApiFunctionRepository.GetEffectiveByUserAsync(tokenSet.OrganizationId, user.UserId, clock.UtcNow, cancellationToken);

        return Result<AuthResponse>.Success(AuthMapper.ToResponse(user, tokenSet, apiFunctions));
    }
}
