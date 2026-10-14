using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using LiveKit.Rtc;
using Nexora.MeetingAgent.Models;

namespace Nexora.MeetingAgent.Tests;

public sealed class BackendClientTests
{
    [Fact]
    public void LiveKitNativeRuntimeLoadsOnWindowsCpu()
    {
        using var room = new Room();
        Assert.NotNull(room);
    }

    [Fact]
    public async Task FinalRetryPreservesSegmentIdAndServiceKey()
    {
        var attempts = new List<(string Path, string Body, string? Key)>();
        using var http = new HttpClient(new StubHandler(async request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.EndsWith("transcription-jobs"))
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            attempts.Add((request.RequestUri!.AbsolutePath, await request.Content!.ReadAsStringAsync(),
                request.Headers.GetValues("X-Meeting-Agent-Key").Single()));
            return new HttpResponseMessage(attempts.Count == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        }));
        var client = new BackendClient(http, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["MeetingAgent:BackendUrl"] = "http://localhost",
                ["MeetingAgent:ApiKey"] = "test-key" }).Build());
        Assert.Empty(await client.JobsAsync(CancellationToken.None));
        var id = Guid.NewGuid();
        await client.SaveFinalAsync(Guid.NewGuid(), new TranscriptEvent("final", id, Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow, "vi", "Xin chào"), CancellationToken.None);

        Assert.Equal(2, attempts.Count);
        Assert.All(attempts, attempt => Assert.Equal("test-key", attempt.Key));
        Assert.Equal(attempts[0].Path, attempts[1].Path);
        Assert.Equal(attempts[0].Body, attempts[1].Body);
        Assert.Contains(id.ToString("D"), attempts[0].Path);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }
}
