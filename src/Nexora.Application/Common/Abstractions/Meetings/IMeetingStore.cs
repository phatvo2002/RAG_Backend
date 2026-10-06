using Nexora.Domain.Meetings;

namespace Nexora.Application.Common.Abstractions.Meetings;

public interface IMeetingStore
{
    Task<Meeting?> GetAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Meeting>> ListAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task<bool> IsParticipantAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken);
    Task<bool> AreActiveMembersAsync(Guid organizationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
    void Add(Meeting meeting, IReadOnlyCollection<MeetingParticipant> participants);
    Task SaveAsync(CancellationToken cancellationToken);
}
