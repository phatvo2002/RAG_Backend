using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Model;
using Nexora.Domain.Meetings;

namespace Nexora.Application.Features.V1.Meetings.Command;

public sealed record CreateMeetingCommand(string Title, DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, IReadOnlyList<Guid> ParticipantUserIds) : IRequest<Result<MeetingResponse>>;

public sealed class CreateMeetingCommandHandler(IMeetingStore store, IUserContext user, ITenantContext tenant, IClock clock) :
    IRequestHandler<CreateMeetingCommand, Result<MeetingResponse>>
{
    public async Task<Result<MeetingResponse>> Handle(CreateMeetingCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<MeetingResponse>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 300 || request.ScheduledStart >= request.ScheduledEnd)
            return Result<MeetingResponse>.Failure(Error.Validation("Tên hoặc thời gian cuộc họp không hợp lệ."));

        var participants = (request.ParticipantUserIds ?? Array.Empty<Guid>()).Append(userId).Distinct().ToArray();
        if (participants.Any(id => id == Guid.Empty) || !await store.AreActiveMembersAsync(organizationId, participants, cancellationToken))
            return Result<MeetingResponse>.Failure(Error.Validation("Người tham gia phải là thành viên đang hoạt động của tổ chức."));

        var meeting = Meeting.Create(organizationId, userId, request.Title, request.ScheduledStart, request.ScheduledEnd, clock.UtcNow);
        store.Add(meeting, participants.Select(id => MeetingParticipant.Create(organizationId, meeting.Id, id)).ToArray());
        await store.SaveAsync(cancellationToken);
        return Result<MeetingResponse>.Success(MeetingResponse.FromMeeting(meeting));
    }
}
