using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Security;

namespace Nexora.Tests.Domain;

public sealed class AuthenticationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateRefreshToken_WithValidHash_ShouldBeActive()
    {
        var token = AccessToken.CreateRefreshToken(Guid.NewGuid(), Guid.NewGuid(), new byte[32], "refresh-token-id", Now, Now.AddDays(30), null, "test-agent");

        Assert.Equal("REFRESH", token.TokenType);
        Assert.True(token.IsActiveAt(Now.AddMinutes(1)));
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void CreateRefreshToken_WithInvalidHash_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            AccessToken.CreateRefreshToken(Guid.NewGuid(), Guid.NewGuid(), new byte[16], "refresh-token-id", Now, Now.AddDays(30), null, null));

        Assert.Equal("access_token.invalid_hash", exception.Code);
    }

    [Fact]
    public void Revoke_ShouldMakeRefreshTokenInactive()
    {
        var token = AccessToken.CreateRefreshToken(Guid.NewGuid(), Guid.NewGuid(), new byte[32], "refresh-token-id", Now, Now.AddDays(30), null, null);

        token.Revoke(token.UserId, Now.AddMinutes(1), "logout");

        Assert.False(token.IsActiveAt(Now.AddMinutes(2)));
        Assert.Equal("logout", token.RevokeReason);
    }
}
