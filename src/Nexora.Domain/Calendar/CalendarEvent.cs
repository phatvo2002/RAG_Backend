using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Calendar;

public class CalendarEvent : SoftDeletableEntityBase
{
    protected CalendarEvent()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? MeetingId { get; private set; }

    public Guid OrganizerUserId { get; private set; }

    public string Title { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public bool AllDay { get; set; }

    public string? Location { get; set; }

    public byte[]? DescriptionCiphertext { get; set; }

    public string Status { get; set; } = string.Empty;

}
