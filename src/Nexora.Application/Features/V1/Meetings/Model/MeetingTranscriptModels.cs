using Nexora.Domain.Meetings;

namespace Nexora.Application.Features.V1.Meetings.Model;

public sealed record SetMeetingSourceLanguageRequest(string Language);
public sealed record SaveMeetingTranscriptRequest(Guid SegmentId, Guid SpeakerUserId,
    DateTimeOffset StartedAt, DateTimeOffset EndedAt, string SourceLanguage, string Text);
public sealed record MeetingTranscriptResponse(Guid SegmentId, Guid SpeakerUserId, DateTimeOffset StartedAt,
    DateTimeOffset EndedAt, string SourceLanguage, string Text)
{
    public static MeetingTranscriptResponse FromSegment(MeetingTranscriptSegment segment) =>
        new(segment.Id, segment.SpeakerUserId, segment.StartedAt, segment.EndedAt, segment.SourceLanguage, segment.Text);
}
