namespace Nexora.Application.Features.V1.Meetings.Model;

public sealed record CreateMeetingRequest(string Title, DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, IReadOnlyList<Guid> ParticipantUserIds);

public sealed record MeetingResponse(Guid Id, string Title, string Status, Guid OrganizerUserId,
    DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, DateTimeOffset? ActualStartedAt, DateTimeOffset? ActualEndedAt);
