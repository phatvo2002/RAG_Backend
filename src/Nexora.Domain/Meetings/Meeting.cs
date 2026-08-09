using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Meetings;

public class Meeting : SoftDeletableEntityBase
{
    protected Meeting()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid OrganizerUserId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset ScheduledStart { get; set; }

    public DateTimeOffset ScheduledEnd { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? RoomProvider { get; set; }

    public string? RoomExternalId { get; set; }

    public string? RecordingConsentStatus { get; set; }

}
