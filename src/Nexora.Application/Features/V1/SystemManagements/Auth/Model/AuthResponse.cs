using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Model;

public sealed record AuthResponse
{
    public AuthResponse()
    {
    }

    public AuthResponse(
        Guid userId,
        string email,
        string tokenType,
        string accessToken,
        DateTimeOffset accessTokenExpiresAt,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        IReadOnlyList<ApiFunctionResponse>? apiFunctions = null)
    {
        UserId = userId;
        Email = email;
        TokenType = tokenType;
        AccessToken = accessToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        RefreshToken = refreshToken;
        RefreshTokenExpiresAt = refreshTokenExpiresAt;
     //   ApiFunctions = apiFunctions ?? Array.Empty<ApiFunctionResponse>();
    }

    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
   // public IReadOnlyList<ApiFunctionResponse> ApiFunctions { get; set; } = Array.Empty<ApiFunctionResponse>();
}

public sealed record CurrentUserResponse
{
    public CurrentUserResponse()
    {
    }

    public CurrentUserResponse(
        Guid userId,
        string email,
        string userName,
        string? displayName,
        IReadOnlyList<ApiFunctionMenuResponse>? allowMenuTree = null)
    {
        UserId = userId;
        Email = email;
        UserName = userName;
        DisplayName = displayName;
        AllowMenuTree = allowMenuTree ?? Array.Empty<ApiFunctionMenuResponse>();
    }

    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public IReadOnlyList<ApiFunctionMenuResponse> AllowMenuTree { get; set; } = Array.Empty<ApiFunctionMenuResponse>();
}

public sealed record LoginRequest
{
    public LoginRequest()
    {
    }

    public LoginRequest(string email, string password)
    {
        Email = email;
        Password = password;
    }

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record RefreshTokenRequest
{
    public RefreshTokenRequest()
    {
    }

    public RefreshTokenRequest(string refreshToken)
    {
        RefreshToken = refreshToken;
    }

    public string RefreshToken { get; set; } = string.Empty;
}

public sealed record LogoutRequest
{
    public LogoutRequest()
    {
    }

    public LogoutRequest(string refreshToken)
    {
        RefreshToken = refreshToken;
    }

    public string RefreshToken { get; set; } = string.Empty;
}
