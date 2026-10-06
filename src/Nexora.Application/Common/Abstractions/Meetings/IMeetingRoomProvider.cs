namespace Nexora.Application.Common.Abstractions.Meetings;

public interface IMeetingRoomProvider
{
    Task CreateAsync(string roomName, CancellationToken cancellationToken);
    Task DeleteAsync(string roomName, CancellationToken cancellationToken);
    MeetingJoinToken IssueToken(string roomName, Guid userId);
}

public sealed record MeetingJoinToken(string Url, string Token);
