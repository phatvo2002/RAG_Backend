using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Model;

namespace Nexora.Application.Features.V1.Meetings.Command;

public sealed record EndMeetingCommand(Guid MeetingId) : IRequest<Result<MeetingResponse>>;

public sealed class EndMeetingCommandHandler(IMeetingStore store, IMeetingRoomProvider rooms, IUserContext user, ITenantContext tenant, IClock clock) :
    IRequestHandler<EndMeetingCommand, Result<MeetingResponse>>
{
    public async Task<Result<MeetingResponse>> Handle(EndMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingHostAccess.GetForHostAsync(store, user, tenant, request.MeetingId, cancellationToken);
        if (meeting is null) return Result<MeetingResponse>.Failure(Error.Forbidden("Không có quyền kết thúc cuộc họp."));
        if (meeting.Status is not ("Live" or "Ending")) return Result<MeetingResponse>.Failure(Error.Conflict("Cuộc họp chưa bắt đầu hoặc đã kết thúc."));
        meeting.BeginEnding();
        await store.SaveAsync(cancellationToken);
        await rooms.DeleteAsync(meeting.RoomExternalId!, cancellationToken);
        meeting.Finish(clock.UtcNow);
        await store.SaveAsync(cancellationToken);
        return Result<MeetingResponse>.Success(MeetingResponse.FromMeeting(meeting));
    }
}
