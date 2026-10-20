using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nexora.Application.Common.Abstractions.Meetings;

namespace Nexora.Infrastructure.Services.Integrations.LiveKit;

internal sealed class LiveKitRoomProvider(HttpClient client, IConfiguration configuration) : IMeetingRoomProvider
{
    private string Url => configuration["LiveKit:Url"] ?? throw new InvalidOperationException("LiveKit:Url chưa được cấu hình.");
    private string Key => configuration["LiveKit:ApiKey"] ?? throw new InvalidOperationException("LiveKit:ApiKey chưa được cấu hình.");
    private string Secret => configuration["LiveKit:ApiSecret"] ?? throw new InvalidOperationException("LiveKit:ApiSecret chưa được cấu hình.");

    public async Task CreateAsync(string roomName, CancellationToken cancellationToken) =>
        await SendAsync("CreateRoom", roomName, new { name = roomName }, cancellationToken, allowConflict: true);

    public async Task DeleteAsync(string roomName, CancellationToken cancellationToken) =>
        await SendAsync("DeleteRoom", roomName, new { room = roomName }, cancellationToken, allowNotFound: true);

    public MeetingJoinToken IssueToken(string roomName, Guid userId) => new(Url, Sign(userId.ToString("N"), new Dictionary<string, object>
    {
        ["roomJoin"] = true, ["room"] = roomName, ["canPublish"] = true, ["canSubscribe"] = true
    }));

    public MeetingJoinToken IssueTranscriptWorkerToken(string roomName, Guid meetingId) =>
        new(Url, Sign($"transcript-agent-{meetingId:N}", new Dictionary<string, object>
        {
            ["roomJoin"] = true, ["room"] = roomName, ["canPublish"] = false,
            ["canPublishData"] = true, ["canSubscribe"] = true
        }));

    private async Task SendAsync(string method, string roomName, object body, CancellationToken cancellationToken, bool allowNotFound = false, bool allowConflict = false)
    {
        var endpoint = new Uri(Url.Replace("wss://", "https://", StringComparison.OrdinalIgnoreCase)
            .Replace("ws://", "http://", StringComparison.OrdinalIgnoreCase).TrimEnd('/') + "/twirp/livekit.RoomService/" + method);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(body)
        };
        var grant = new Dictionary<string, object> { ["roomCreate"] = true };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Sign("meeting-service", grant));
        using var response = await client.SendAsync(request, cancellationToken);
        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound) return;
        if (allowConflict && response.StatusCode == HttpStatusCode.Conflict) return;
        response.EnsureSuccessStatusCode();
    }

    private string Sign(string identity, Dictionary<string, object> videoGrant)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new JwtPayload
        {
            ["iss"] = Key, ["sub"] = identity, ["nbf"] = now.ToUnixTimeSeconds(),
            ["iat"] = now.ToUnixTimeSeconds(), ["exp"] = now.AddMinutes(2).ToUnixTimeSeconds(),
            ["video"] = videoGrant
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(credentials), payload));
    }
}
