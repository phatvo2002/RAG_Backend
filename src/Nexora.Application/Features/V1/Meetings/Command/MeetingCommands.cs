using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Model;
using Nexora.Domain.Meetings;

namespace Nexora.Application.Features.V1.Meetings.Command;

public sealed record CreateMeetingCommand(string Title, DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, IReadOnlyList<Guid> ParticipantUserIds) : IRequest<Result<MeetingResponse>>;
public sealed record StartMeetingCommand(Guid MeetingId) : IRequest<Result<MeetingResponse>>;
public sealed record EndMeetingCommand(Guid MeetingId) : IRequest<Result<MeetingResponse>>;
public sealed record JoinMeetingCommand(Guid MeetingId) : IRequest<Result<MeetingJoinToken>>;

public sealed class MeetingCommandHandler(IMeetingStore store, IMeetingRoomProvider rooms, IUserContext user, ITenantContext tenant, IClock clock) :
    IRequestHandler<CreateMeetingCommand, Result<MeetingResponse>>,
    IRequestHandler<StartMeetingCommand, Result<MeetingResponse>>,
    IRequestHandler<EndMeetingCommand, Result<MeetingResponse>>,
    IRequestHandler<JoinMeetingCommand, Result<MeetingJoinToken>>
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
        return Result<MeetingResponse>.Success(ToResponse(meeting));
    }

    public async Task<Result<MeetingResponse>> Handle(StartMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await GetForHostAsync(request.MeetingId, cancellationToken);
        if (meeting is null) return Result<MeetingResponse>.Failure(Error.Forbidden("Không có quyền bắt đầu cuộc họp."));
        if (meeting.Status != "Scheduled") return Result<MeetingResponse>.Failure(Error.Conflict("Cuộc họp không ở trạng thái chờ."));
        await rooms.CreateAsync(meeting.RoomExternalId!, cancellationToken);
        meeting.Start(clock.UtcNow);
        await store.SaveAsync(cancellationToken);
        return Result<MeetingResponse>.Success(ToResponse(meeting));
    }

    public async Task<Result<MeetingResponse>> Handle(EndMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await GetForHostAsync(request.MeetingId, cancellationToken);
        if (meeting is null) return Result<MeetingResponse>.Failure(Error.Forbidden("Không có quyền kết thúc cuộc họp."));
        if (meeting.Status is not ("Live" or "Ending")) return Result<MeetingResponse>.Failure(Error.Conflict("Cuộc họp chưa bắt đầu hoặc đã kết thúc."));
        meeting.BeginEnding();
        await store.SaveAsync(cancellationToken);
        await rooms.DeleteAsync(meeting.RoomExternalId!, cancellationToken);
        meeting.Finish(clock.UtcNow);
        await store.SaveAsync(cancellationToken);
        return Result<MeetingResponse>.Success(ToResponse(meeting));
    }

    public async Task<Result<MeetingJoinToken>> Handle(JoinMeetingCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<MeetingJoinToken>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        var meeting = await store.GetAsync(organizationId, request.MeetingId, cancellationToken);
        if (meeting is null) return Result<MeetingJoinToken>.Failure(Error.NotFound("Không tìm thấy cuộc họp."));
        if (!await store.IsParticipantAsync(organizationId, request.MeetingId, userId, cancellationToken))
            return Result<MeetingJoinToken>.Failure(Error.Forbidden("Bạn không có trong danh sách tham gia."));
        if (meeting.Status != "Live") return Result<MeetingJoinToken>.Failure(Error.Conflict("Cuộc họp chưa bắt đầu hoặc đang kết thúc."));
        return Result<MeetingJoinToken>.Success(rooms.IssueToken(meeting.RoomExternalId!, userId));
    }

    private async Task<Meeting?> GetForHostAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId) return null;
        var meeting = await store.GetAsync(organizationId, meetingId, cancellationToken);
        return meeting?.OrganizerUserId == userId && await store.IsParticipantAsync(organizationId, meetingId, userId, cancellationToken) ? meeting : null;
    }

    internal static MeetingResponse ToResponse(Meeting meeting) => new(meeting.Id, meeting.Title, meeting.Status,
        meeting.OrganizerUserId, meeting.ScheduledStart, meeting.ScheduledEnd, meeting.ActualStartedAt, meeting.ActualEndedAt);
}
