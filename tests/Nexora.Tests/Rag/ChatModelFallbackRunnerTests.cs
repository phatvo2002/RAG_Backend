using System.Net;
using System.Runtime.CompilerServices;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.WebApi.Controllers.V1.Chat;

namespace Nexora.Tests.Rag;

public sealed class ChatModelFallbackRunnerTests
{
    [Fact]
    public async Task Retries_next_version_from_same_provider_only()
    {
        var candidates = new[]
        {
            new ChatModelChoice("primary", "Gemini primary", "Gemini", "gemini-primary"),
            new ChatModelChoice("fallback", "Gemini fallback", "Gemini", "gemini-fallback"),
            new ChatModelChoice("claude", "Claude", "Claude", "claude-model"),
        };
        var attempted = new List<string>();

        var result = await ChatModelFallbackRunner.RunAsync(
            candidates,
            (model, _) =>
            {
                attempted.Add(model.Id);
                return model.Id == "primary"
                    ? Task.FromException<IAsyncEnumerable<string>>(new HttpRequestException("Unavailable", null, HttpStatusCode.ServiceUnavailable))
                    : Task.FromResult(Values("Answer"));
            },
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _, _) => { },
            CancellationToken.None);

        Assert.Equal(new[] { "primary", "fallback" }, attempted);
        Assert.Equal("fallback", result.Model.Id);
        Assert.Equal("Answer", result.Answer);
    }

    [Fact]
    public async Task Clears_partial_output_before_retrying_a_midstream_failure()
    {
        var candidates = new[]
        {
            new ChatModelChoice("primary", "Gemini primary", "Gemini", "gemini-primary"),
            new ChatModelChoice("fallback", "Gemini fallback", "Gemini", "gemini-fallback"),
        };
        var events = new List<string>();

        var result = await ChatModelFallbackRunner.RunAsync(
            candidates,
            (model, _) => Task.FromResult(model.Id == "primary" ? PartialThenFailure() : Values("Replacement answer")),
            (delta, _) => { events.Add($"delta:{delta}"); return Task.CompletedTask; },
            (model, _) => { events.Add($"reset:{model.Name}"); return Task.CompletedTask; },
            (_, _, _) => { },
            CancellationToken.None);

        Assert.Equal(new[] { "delta:partial", "reset:Gemini fallback", "delta:Replacement answer" }, events);
        Assert.Equal("Replacement answer", result.Answer);
        Assert.Equal("fallback", result.Model.Id);
    }

    [Fact]
    public async Task Throws_after_every_same_provider_version_fails()
    {
        var candidates = new[]
        {
            new ChatModelChoice("primary", "Gemini primary", "Gemini", "gemini-primary"),
            new ChatModelChoice("fallback", "Gemini fallback", "Gemini", "gemini-fallback"),
        };
        var attempted = new List<string>();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => ChatModelFallbackRunner.RunAsync(
            candidates,
            (model, _) =>
            {
                attempted.Add(model.Id);
                return Task.FromException<IAsyncEnumerable<string>>(new HttpRequestException(
                    $"{model.Id} unavailable", null, HttpStatusCode.ServiceUnavailable));
            },
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _, _) => { },
            CancellationToken.None));

        Assert.Equal(new[] { "primary", "fallback" }, attempted);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task Does_not_retry_authentication_or_invalid_request_failures(HttpStatusCode statusCode)
    {
        var candidates = new[]
        {
            new ChatModelChoice("primary", "Gemini primary", "Gemini", "gemini-primary"),
            new ChatModelChoice("fallback", "Gemini fallback", "Gemini", "gemini-fallback"),
        };
        var attempts = 0;

        await Assert.ThrowsAsync<HttpRequestException>(() => ChatModelFallbackRunner.RunAsync(
            candidates,
            (_, _) =>
            {
                attempts++;
                return Task.FromException<IAsyncEnumerable<string>>(new HttpRequestException("Provider rejected the request", null, statusCode));
            },
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _, _) => { },
            CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    private static async IAsyncEnumerable<string> Values(params string[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<string> PartialThenFailure([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return "partial";
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        throw new HttpRequestException("The generation provider reported a streaming error.");
    }
}
