using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Model;

namespace Nexora.Application.Features.V1.Meetings.Command;

public sealed record UpdateMeetingCommand(Guid MeetingId, string Title, DateTimeOffset ScheduledStart,
    DateTimeOffset ScheduledEnd, IReadOnlyList<Guid> ParticipantUserIds, long ExpectedRowVersion)
    : IRequest<Result<MeetingDetailResponse>>;

public sealed class UpdateMeetingCommandHandler(IMeetingStore store, IUserContext user, ITenantContext tenant)
    : IRequestHandler<UpdateMeetingCommand, Result<MeetingDetailResponse>>
{
    public async Task<Result<MeetingDetailResponse>> Handle(UpdateMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingHostAccess.GetForHostAsync(store, user, tenant, request.MeetingId, cancellationToken);
        if (meeting is null || tenant.OrganizationId is not { } organizationId)
            return Result<MeetingDetailResponse>.Failure(Error.Forbidden("Không có quyền sửa cuộc họp."));
        if (meeting.Status != "Scheduled" || meeting.RowVersion != request.ExpectedRowVersion)
            return Result<MeetingDetailResponse>.Failure(Error.Conflict("Cuộc họp đã thay đổi. Vui lòng tải lại."));
        var userIds = (request.ParticipantUserIds ?? Array.Empty<Guid>())
            .Append(meeting.OrganizerUserId).Distinct().ToArray();
        if (userIds.Any(id => id == Guid.Empty) ||
            !await store.AreActiveMembersAsync(organizationId, userIds, cancellationToken))
            return Result<MeetingDetailResponse>.Failure(Error.Validation("Người tham gia không hợp lệ."));

        meeting.ReviseScheduledDetails(request.Title, request.ScheduledStart, request.ScheduledEnd);
        await store.SyncParticipantUsersAsync(meeting, userIds, cancellationToken);
        if (!await store.TrySaveUpdateAsync(cancellationToken))
            return Result<MeetingDetailResponse>.Failure(Error.Conflict("Cuộc họp đã thay đổi. Vui lòng tải lại."));

        var participants = await store.GetParticipantUsersAsync(organizationId, meeting.Id, cancellationToken);
        return Result<MeetingDetailResponse>.Success(MeetingDetailResponse.FromMeeting(meeting, participants));
    }
}
