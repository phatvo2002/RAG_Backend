using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Model;

namespace Nexora.Application.Features.V1.Meetings.Command;

public sealed record StartMeetingCommand(Guid MeetingId) : IRequest<Result<MeetingResponse>>;

public sealed class StartMeetingCommandHandler(IMeetingStore store, IMeetingRoomProvider rooms, IUserContext user, ITenantContext tenant, IClock clock) :
    IRequestHandler<StartMeetingCommand, Result<MeetingResponse>>
{
    public async Task<Result<MeetingResponse>> Handle(StartMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingHostAccess.GetForHostAsync(store, user, tenant, request.MeetingId, cancellationToken);
        if (meeting is null) return Result<MeetingResponse>.Failure(Error.Forbidden("Không có quyền bắt đầu cuộc họp."));
        if (meeting.Status != "Scheduled") return Result<MeetingResponse>.Failure(Error.Conflict("Cuộc họp không ở trạng thái chờ."));
        await rooms.CreateAsync(meeting.RoomExternalId!, cancellationToken);
        meeting.Start(clock.UtcNow);
        if (!await store.TrySaveUpdateAsync(cancellationToken))
            return Result<MeetingResponse>.Failure(Error.Conflict("Cuộc họp đã thay đổi. Vui lòng thử lại."));
        return Result<MeetingResponse>.Success(MeetingResponse.FromMeeting(meeting));
    }
}
