namespace Nexora.Application.Common.Abstractions.Auth;

public interface IIdentityService
{
    Task<IdentityUserSnapshot?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<IdentityUserSnapshot?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed record IdentityUserSnapshot(Guid UserId, string Email, string UserName);
