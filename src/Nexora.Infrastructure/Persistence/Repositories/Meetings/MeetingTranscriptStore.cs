using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Domain.Meetings;
using Nexora.Domain.Organizations.Enums;
using Npgsql;

namespace Nexora.Infrastructure.Persistence.Repositories.Meetings;

internal sealed class MeetingTranscriptStore(NexoraDbContext db) : IMeetingTranscriptStore
{
    public Task<string?> GetMyLanguageAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
        db.MeetingParticipants.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.MeetingId == meetingId && p.UserId == userId && p.GuestEmail == null)
            .Select(p => p.SourceLanguage).SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> SetMyLanguageAsync(Guid organizationId, Guid meetingId, Guid userId, string language, CancellationToken cancellationToken)
    {
        var participant = await db.MeetingParticipants.SingleOrDefaultAsync(p => p.OrganizationId == organizationId &&
            p.MeetingId == meetingId && p.UserId == userId && p.GuestEmail == null, cancellationToken);
        if (participant is null) return false;
        participant.SourceLanguage = language;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MeetingTranscriptSegment>> ListAsync(Guid organizationId, Guid meetingId, CancellationToken cancellationToken) =>
        await db.MeetingTranscriptSegments.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && s.MeetingId == meetingId)
            .OrderBy(s => s.StartedAt).ThenBy(s => s.Id).ToListAsync(cancellationToken);

    public Task<Meeting?> FindMeetingForAgentAsync(Guid meetingId, CancellationToken cancellationToken) =>
        db.Meetings.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == meetingId && m.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<Meeting>> ListActiveForAgentAsync(CancellationToken cancellationToken) =>
        await db.Meetings.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.DeletedAt == null && (m.Status == "Live" || m.Status == "Ending") && m.RoomExternalId != null)
            .OrderBy(m => m.Id).ToListAsync(cancellationToken);

    public Task<string?> GetSpeakerLanguageForAgentAsync(Guid organizationId, Guid meetingId, Guid userId, CancellationToken cancellationToken) =>
        db.MeetingParticipants.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.MeetingId == meetingId && p.UserId == userId && p.GuestEmail == null &&
                db.OrganizationMemberships.IgnoreQueryFilters().Any(m => m.OrganizationId == organizationId &&
                    m.UserId == userId && m.Status == OrganizationMembershipStatus.Active) &&
                db.Users.Any(u => u.Id == userId && u.IsActive))
            .Select(p => p.SourceLanguage).SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> SaveFinalAsync(MeetingTranscriptSegment segment, CancellationToken cancellationToken)
    {
        var existing = await FindExistingAsync(segment, cancellationToken);
        if (existing is not null) return Same(existing, segment);
        db.MeetingTranscriptSegments.Add(segment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(segment).State = EntityState.Detached;
            existing = await FindExistingAsync(segment, cancellationToken);
            return existing is not null && Same(existing, segment);
        }
    }

    private Task<MeetingTranscriptSegment?> FindExistingAsync(MeetingTranscriptSegment segment, CancellationToken cancellationToken) =>
        db.MeetingTranscriptSegments.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == segment.Id && s.MeetingId == segment.MeetingId &&
                s.OrganizationId == segment.OrganizationId, cancellationToken);

    private static bool Same(MeetingTranscriptSegment left, MeetingTranscriptSegment right) =>
        left.SpeakerUserId == right.SpeakerUserId && left.StartedAt == right.StartedAt && left.EndedAt == right.EndedAt &&
        left.SourceLanguage == right.SourceLanguage && left.Text == right.Text;
}
