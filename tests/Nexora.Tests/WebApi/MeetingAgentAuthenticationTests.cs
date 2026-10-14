using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.WebApi.Authentication;
using Nexora.WebApi.Controllers.Internal.Meetings;

namespace Nexora.Tests.WebApi;

public sealed class MeetingAgentAuthenticationTests
{
    [Fact]
    public async Task InternalEndpointRequiresServiceKeyEvenWithBrowserBearerToken()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["MeetingAgent:ApiKey"] = "test-service-secret" }).Build());
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, MeetingAgentAuthenticationHandler>(
            MeetingAgentAuthenticationHandler.SchemeName, _ => { });
        using var provider = services.BuildServiceProvider();
        async Task<bool> Authenticate(string? serviceKey)
        {
            using var scope = provider.CreateScope();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Headers.Authorization = "Bearer browser-token";
            if (serviceKey is not null)
                context.Request.Headers["X-Meeting-Agent-Key"] = serviceKey;
            return (await context.AuthenticateAsync(MeetingAgentAuthenticationHandler.SchemeName)).Succeeded;
        }

        Assert.False(await Authenticate(null));
        Assert.False(await Authenticate("wrong"));
        Assert.True(await Authenticate("test-service-secret"));
    }

    [Fact]
    public void InternalControllerUsesServiceScheme()
    {
        var attribute = Assert.Single(typeof(MeetingAgentTranscriptController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(MeetingAgentAuthenticationHandler.SchemeName, attribute.AuthenticationSchemes);
    }
}
