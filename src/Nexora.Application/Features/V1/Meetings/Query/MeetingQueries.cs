using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Command;
using Nexora.Application.Features.V1.Meetings.Model;

namespace Nexora.Application.Features.V1.Meetings.Query;

public sealed record ListMeetingsQuery : IRequest<Result<IReadOnlyList<MeetingResponse>>>;
public sealed record GetMeetingQuery(Guid MeetingId) : IRequest<Result<MeetingResponse>>;

public sealed class MeetingQueryHandler(IMeetingStore store, IUserContext user, ITenantContext tenant) :
    IRequestHandler<ListMeetingsQuery, Result<IReadOnlyList<MeetingResponse>>>,
    IRequestHandler<GetMeetingQuery, Result<MeetingResponse>>
{
    public async Task<Result<IReadOnlyList<MeetingResponse>>> Handle(ListMeetingsQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<IReadOnlyList<MeetingResponse>>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        var meetings = await store.ListAsync(organizationId, userId, cancellationToken);
        return Result<IReadOnlyList<MeetingResponse>>.Success(meetings.Select(MeetingCommandHandler.ToResponse).ToArray());
    }

    public async Task<Result<MeetingResponse>> Handle(GetMeetingQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<MeetingResponse>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        var meeting = await store.GetAsync(organizationId, request.MeetingId, cancellationToken);
        if (meeting is null) return Result<MeetingResponse>.Failure(Error.NotFound("Không tìm thấy cuộc họp."));
        if (!await store.IsParticipantAsync(organizationId, meeting.Id, userId, cancellationToken))
            return Result<MeetingResponse>.Failure(Error.Forbidden("Không có quyền xem cuộc họp."));
        return Result<MeetingResponse>.Success(MeetingCommandHandler.ToResponse(meeting));
    }
}
