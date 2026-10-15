using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Domain.Meetings;

namespace Nexora.Application.Features.V1.Meetings.Command;

internal static class MeetingHostAccess
{
    internal static async Task<Meeting?> GetForHostAsync(IMeetingStore store, IUserContext user, ITenantContext tenant,
        Guid meetingId, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId) return null;
        var meeting = await store.GetAsync(organizationId, meetingId, cancellationToken);
        return meeting?.OrganizerUserId == userId && await store.IsParticipantAsync(organizationId, meetingId, userId, cancellationToken) ? meeting : null;
    }
}
