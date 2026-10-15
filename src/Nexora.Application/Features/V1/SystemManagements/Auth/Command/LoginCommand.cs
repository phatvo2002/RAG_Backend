using MediatR;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Auth.Mapper;
using Nexora.Application.Features.V1.SystemManagements.Auth.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Command;

public sealed record LoginCommand(string Email, string Password) : ITransactionalCommand<Result<AuthResponse>>;

public sealed class LoginCommandHandler(IUnitOfWork unitOfWork, IAuthTokenService authTokenService, IHttpRequestMetadata requestMetadata, IClock clock) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        try
        {
            if (user is null)
            {
                return Result<AuthResponse>.Failure(
                    new Error(CommonMessages.ErrorCodes.InvalidCredentials, CommonMessages.Auth.InvalidCredentials));
            }

            var tokenSet = await authTokenService.IssueAsync(user, requestMetadata.IpAddress, requestMetadata.UserAgent, cancellationToken);
            //     var apiFunctions = await unitOfWork.ApiFunctionRepository.GetEffectiveByUserAsync(tokenSet.OrganizationId, user.UserId, clock.UtcNow, cancellationToken);

            return Result<AuthResponse>.Success(AuthMapper.ToResponse(user, tokenSet));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while validating credentials: {ex.Message}");
            return Result<AuthResponse>.Failure(
                new Error(CommonMessages.ErrorCodes.Server, ex.Message));
        }
     
    }
}
