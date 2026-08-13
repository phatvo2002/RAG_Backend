namespace Nexora.Application.Features.V1.SystemManagements.Auth.Model;

public class AuthResponse
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
        DateTimeOffset refreshTokenExpiresAt)
    {
        UserId = userId;
        Email = email;
        TokenType = tokenType;
        AccessToken = accessToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        RefreshToken = refreshToken;
        RefreshTokenExpiresAt = refreshTokenExpiresAt;
    }

    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
}

public class CurrentUserResponse
{
    public CurrentUserResponse()
    {
    }

    public CurrentUserResponse(Guid userId, string email, string userName, string? displayName)
    {
        UserId = userId;
        Email = email;
        UserName = userName;
        DisplayName = displayName;
    }

    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class LoginRequest
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

public class RefreshTokenRequest
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

public class LogoutRequest
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
