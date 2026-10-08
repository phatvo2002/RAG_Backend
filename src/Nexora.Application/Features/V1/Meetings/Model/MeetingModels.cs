using Nexora.Domain.Meetings;

namespace Nexora.Application.Features.V1.Meetings.Model;

public sealed record CreateMeetingRequest(string Title, DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, IReadOnlyList<Guid> ParticipantUserIds);

public sealed record MeetingResponse(Guid Id, string Title, string Status, Guid OrganizerUserId,
    DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, DateTimeOffset? ActualStartedAt, DateTimeOffset? ActualEndedAt)
{
    internal static MeetingResponse FromMeeting(Meeting meeting) => new(meeting.Id, meeting.Title, meeting.Status,
        meeting.OrganizerUserId, meeting.ScheduledStart, meeting.ScheduledEnd, meeting.ActualStartedAt, meeting.ActualEndedAt);
}
