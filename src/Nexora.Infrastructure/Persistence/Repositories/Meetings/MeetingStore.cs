using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Domain.Meetings;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Infrastructure.Persistence.Repositories.Meetings;

internal sealed class MeetingStore(NexoraDbContext dbContext) : IMeetingStore
{
    public Task<Meeting?> GetAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken) =>
        dbContext.Meetings.SingleOrDefaultAsync(m => m.Id == meetingId && m.OrganizationId == organizationId, cancellationToken);

    public async Task<IReadOnlyList<Meeting>> ListAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Meetings.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId &&
                dbContext.OrganizationMemberships.Any(member => member.OrganizationId == organizationId && member.UserId == userId && member.Status == OrganizationMembershipStatus.Active) &&
                (m.OrganizerUserId == userId || dbContext.MeetingParticipants.Any(p => p.MeetingId == m.Id && p.OrganizationId == organizationId && p.UserId == userId)))
            .OrderByDescending(m => m.ScheduledStart).ToListAsync(cancellationToken);

    public Task<bool> IsParticipantAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.MeetingParticipants.AnyAsync(p => p.OrganizationId == organizationId && p.MeetingId == meetingId && p.UserId == userId &&
            dbContext.OrganizationMemberships.Any(member => member.OrganizationId == organizationId && member.UserId == userId && member.Status == OrganizationMembershipStatus.Active), cancellationToken);

    public async Task<bool> AreActiveMembersAsync(Guid organizationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var count = await dbContext.OrganizationMemberships.AsNoTracking()
            .CountAsync(m => m.OrganizationId == organizationId && userIds.Contains(m.UserId) && m.Status == OrganizationMembershipStatus.Active, cancellationToken);
        return count == userIds.Count;
    }

    public void Add(Meeting meeting, IReadOnlyCollection<MeetingParticipant> participants)
    {
        dbContext.Meetings.Add(meeting);
        dbContext.MeetingParticipants.AddRange(participants);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
