using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Meetings;

public class MeetingParticipant : EntityBase
{
    protected MeetingParticipant()
    {
    }

    private MeetingParticipant(Guid id) : base(id)
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid MeetingId { get; private set; }

    public Guid? UserId { get; private set; }

    public string? GuestEmail { get; set; }

    public string? SourceLanguage { get; set; }

    public string? TargetLanguage { get; set; }

    public DateTimeOffset? JoinedAt { get; set; }

    public DateTimeOffset? LeftAt { get; set; }

    public static MeetingParticipant Create(Guid organizationId, Guid meetingId, Guid userId) => new(Guid.NewGuid())
    {
        OrganizationId = organizationId, MeetingId = meetingId, UserId = userId
    };
}
