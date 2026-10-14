using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Meetings.Command;

public sealed record JoinMeetingCommand(Guid MeetingId) : IRequest<Result<MeetingJoinToken>>;

public sealed class JoinMeetingCommandHandler(IMeetingStore store, IMeetingTranscriptStore transcripts,
    IMeetingRoomProvider rooms, IUserContext user, ITenantContext tenant) :
    IRequestHandler<JoinMeetingCommand, Result<MeetingJoinToken>>
{
    public async Task<Result<MeetingJoinToken>> Handle(JoinMeetingCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<MeetingJoinToken>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        var meeting = await store.GetAsync(organizationId, request.MeetingId, cancellationToken);
        if (meeting is null) return Result<MeetingJoinToken>.Failure(Error.NotFound("Không tìm thấy cuộc họp."));
        if (!await store.IsParticipantAsync(organizationId, request.MeetingId, userId, cancellationToken))
            return Result<MeetingJoinToken>.Failure(Error.Forbidden("Bạn không có trong danh sách tham gia."));
        if (meeting.Status != "Live") return Result<MeetingJoinToken>.Failure(Error.Conflict("Cuộc họp chưa bắt đầu hoặc đang kết thúc."));
        if (await transcripts.GetMyLanguageAsync(organizationId, meeting.Id, userId, cancellationToken) is null)
            return Result<MeetingJoinToken>.Failure(Error.Validation("Vui lòng chọn ngôn ngữ nói trước khi vào phòng."));
        return Result<MeetingJoinToken>.Success(rooms.IssueToken(meeting.RoomExternalId!, userId));
    }
}
