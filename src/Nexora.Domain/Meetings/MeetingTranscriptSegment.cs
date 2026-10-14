using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Meetings;

public sealed class MeetingTranscriptSegment : EntityBase
{
    private MeetingTranscriptSegment() { }

    private MeetingTranscriptSegment(Guid id) : base(id) { }

    public Guid OrganizationId { get; private set; }
    public Guid MeetingId { get; private set; }
    public Guid SpeakerUserId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset EndedAt { get; private set; }
    public string SourceLanguage { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;

    public static MeetingTranscriptSegment Create(Guid segmentId, Guid organizationId, Guid meetingId,
        Guid speakerUserId, DateTimeOffset startedAt, DateTimeOffset endedAt, string language, string text)
    {
        if (segmentId == Guid.Empty || organizationId == Guid.Empty || meetingId == Guid.Empty || speakerUserId == Guid.Empty ||
            startedAt >= endedAt || language is not ("vi" or "en") || string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Đoạn transcript không hợp lệ.");

        return new MeetingTranscriptSegment(segmentId)
        {
            OrganizationId = organizationId, MeetingId = meetingId, SpeakerUserId = speakerUserId,
            StartedAt = startedAt.ToUniversalTime(), EndedAt = endedAt.ToUniversalTime(),
            SourceLanguage = language, Text = text.Trim()
        };
    }
}
