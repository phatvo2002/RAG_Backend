using Nexora.Domain.Meetings;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Abstractions.Meetings;

public interface IMeetingStore
{
    Task<Meeting?> GetAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Meeting>> ListAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task<bool> IsParticipantAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken);
    Task<bool> AreActiveMembersAsync(Guid organizationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<MeetingUserOption>> GetParticipantUsersAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken);
    Task<PagedResult<MeetingUserOption>> FindEligibleUsersAsync(Guid organizationId, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task SyncParticipantUsersAsync(Meeting meeting, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
    Task<bool> TrySaveUpdateAsync(CancellationToken cancellationToken);
    void Add(Meeting meeting, IReadOnlyCollection<MeetingParticipant> participants);
    Task SaveAsync(CancellationToken cancellationToken);
}

public sealed record MeetingUserOption(Guid Id, string DisplayName, string Email);
