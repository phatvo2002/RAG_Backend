using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Nexora.WebApi.Authentication;

internal sealed class MeetingAgentAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "MeetingAgent";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = configuration["MeetingAgent:ApiKey"];
        var supplied = Request.Headers["X-Meeting-Agent-Key"].ToString();
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied))
            return Task.FromResult(AuthenticateResult.Fail("Meeting agent key is missing."));

        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash))
            return Task.FromResult(AuthenticateResult.Fail("Meeting agent key is invalid."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "meeting-agent")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
