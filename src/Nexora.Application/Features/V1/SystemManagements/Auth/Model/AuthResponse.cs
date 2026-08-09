namespace Nexora.Application.Features.V1.SystemManagements.Auth.Model;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    Guid OrganizationId,
    string TokenType,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record CurrentUserResponse(Guid UserId, string Email, string UserName, string? DisplayName, Guid OrganizationId);

public sealed record LoginRequest(string Email, string Password, Guid OrganizationId);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record SelectOrganizationRequest(Guid OrganizationId);
