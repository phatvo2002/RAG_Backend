using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
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
                (m.OrganizerUserId == userId || dbContext.MeetingParticipants.Any(p => p.MeetingId == m.Id && p.OrganizationId == organizationId &&
                    p.UserId == userId && p.GuestEmail == null)))
            .OrderByDescending(m => m.ScheduledStart).ToListAsync(cancellationToken);

    public Task<bool> IsParticipantAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.MeetingParticipants.AnyAsync(p => p.OrganizationId == organizationId && p.MeetingId == meetingId && p.UserId == userId &&
            p.GuestEmail == null &&
            dbContext.OrganizationMemberships.Any(member => member.OrganizationId == organizationId && member.UserId == userId && member.Status == OrganizationMembershipStatus.Active), cancellationToken);

    public async Task<bool> AreActiveMembersAsync(Guid organizationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var count = await dbContext.OrganizationMemberships.AsNoTracking()
            .CountAsync(m => m.OrganizationId == organizationId && userIds.Contains(m.UserId) &&
                m.Status == OrganizationMembershipStatus.Active &&
                dbContext.Users.Any(user => user.Id == m.UserId && user.IsActive), cancellationToken);
        return count == userIds.Count;
    }

    public async Task<IReadOnlyList<MeetingUserOption>> GetParticipantUsersAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken) =>
        await dbContext.MeetingParticipants.AsNoTracking()
            .Where(participant => participant.OrganizationId == organizationId && participant.MeetingId == meetingId &&
                participant.UserId.HasValue && participant.GuestEmail == null)
            .Join(dbContext.Users.AsNoTracking(), participant => participant.UserId!.Value, account => account.Id,
                (participant, account) => account)
            .OrderBy(account => account.Email)
            .Select(account => new MeetingUserOption(account.Id,
                dbContext.UserProfiles.Where(profile => profile.UserId == account.Id).Select(profile => profile.DisplayName).FirstOrDefault()
                    ?? account.UserName ?? account.Email ?? string.Empty,
                account.Email ?? string.Empty))
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<MeetingUserOption>> FindEligibleUsersAsync(Guid organizationId, string? search, int page,
        int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.OrganizationMemberships.AsNoTracking()
            .Where(member => member.OrganizationId == organizationId && member.Status == OrganizationMembershipStatus.Active)
            .Join(dbContext.Users.AsNoTracking().Where(account => account.IsActive),
                member => member.UserId, account => account.Id, (member, account) => account);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(account =>
                EF.Functions.ILike(account.Email ?? string.Empty, pattern) ||
                EF.Functions.ILike(account.UserName ?? string.Empty, pattern) ||
                dbContext.UserProfiles.Any(profile => profile.UserId == account.Id &&
                    EF.Functions.ILike(profile.DisplayName, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(account => account.Email).ThenBy(account => account.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(account => new MeetingUserOption(account.Id,
                dbContext.UserProfiles.Where(profile => profile.UserId == account.Id).Select(profile => profile.DisplayName).FirstOrDefault()
                    ?? account.UserName ?? account.Email ?? string.Empty,
                account.Email ?? string.Empty))
            .ToListAsync(cancellationToken);
        return new PagedResult<MeetingUserOption>(items, page, pageSize, total);
    }

    public async Task SyncParticipantUsersAsync(Meeting meeting, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var current = await dbContext.MeetingParticipants
            .Where(participant => participant.OrganizationId == meeting.OrganizationId && participant.MeetingId == meeting.Id &&
                participant.UserId.HasValue && participant.GuestEmail == null)
            .ToListAsync(cancellationToken);
        var desired = userIds.ToHashSet();
        dbContext.MeetingParticipants.RemoveRange(current.Where(participant => !desired.Contains(participant.UserId!.Value)));
        var existing = current.Select(participant => participant.UserId!.Value).ToHashSet();
        dbContext.MeetingParticipants.AddRange(desired.Except(existing)
            .Select(userId => MeetingParticipant.Create(meeting.OrganizationId, meeting.Id, userId)));
        dbContext.Entry(meeting).Property(entity => entity.UpdatedAt).IsModified = true;
    }

    public async Task<bool> TrySaveUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public void Add(Meeting meeting, IReadOnlyCollection<MeetingParticipant> participants)
    {
        dbContext.Meetings.Add(meeting);
        dbContext.MeetingParticipants.AddRange(participants);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
