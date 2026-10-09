using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Meetings;

public class Meeting : SoftDeletableEntityBase
{
    protected Meeting()
    {
    }

    private Meeting(Guid id, DateTimeOffset now) : base(id, now)
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

    public DateTimeOffset? ActualStartedAt { get; private set; }

    public DateTimeOffset? ActualEndedAt { get; private set; }

    public static Meeting Create(Guid organizationId, Guid organizerUserId, string title, DateTimeOffset scheduledStart, DateTimeOffset scheduledEnd, DateTimeOffset now)
    {
        if (organizationId == Guid.Empty || organizerUserId == Guid.Empty || string.IsNullOrWhiteSpace(title) || scheduledStart >= scheduledEnd)
            throw new ArgumentException("Thông tin cuộc họp không hợp lệ.");

        return new Meeting(Guid.NewGuid(), now)
        {
            OrganizationId = organizationId, OrganizerUserId = organizerUserId,
            Title = title.Trim(), ScheduledStart = scheduledStart.ToUniversalTime(), ScheduledEnd = scheduledEnd.ToUniversalTime(),
            Status = "Scheduled", RoomProvider = "LiveKit", RoomExternalId = Guid.NewGuid().ToString("N")
        };
    }

    public void Start(DateTimeOffset now)
    {
        if (Status != "Scheduled") throw new InvalidOperationException("Cuộc họp không ở trạng thái chờ.");
        Status = "Live";
        ActualStartedAt = now;
    }

    public void ReviseScheduledDetails(string title, DateTimeOffset scheduledStart, DateTimeOffset scheduledEnd)
    {
        if (Status != "Scheduled") throw new InvalidOperationException("Chỉ có thể sửa cuộc họp chưa bắt đầu.");
        if (string.IsNullOrWhiteSpace(title) || title.Length > 300 || scheduledStart >= scheduledEnd)
            throw new ArgumentException("Thông tin cuộc họp không hợp lệ.");

        Title = title.Trim();
        ScheduledStart = scheduledStart.ToUniversalTime();
        ScheduledEnd = scheduledEnd.ToUniversalTime();
    }

    public void BeginEnding()
    {
        if (Status == "Ending") return;
        if (Status != "Live") throw new InvalidOperationException("Cuộc họp chưa bắt đầu hoặc đã kết thúc.");
        Status = "Ending";
    }

    public void Finish(DateTimeOffset now)
    {
        if (Status != "Ending") throw new InvalidOperationException("Cuộc họp chưa được đóng.");
        Status = "Ended";
        ActualEndedAt = now;
    }

}
