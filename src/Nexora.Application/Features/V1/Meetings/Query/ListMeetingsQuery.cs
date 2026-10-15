using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Meetings.Model;

namespace Nexora.Application.Features.V1.Meetings.Query;

public sealed record ListMeetingsQuery : IRequest<Result<IReadOnlyList<MeetingResponse>>>;

public sealed class ListMeetingsQueryHandler(IMeetingStore store, IUserContext user, ITenantContext tenant) :
    IRequestHandler<ListMeetingsQuery, Result<IReadOnlyList<MeetingResponse>>>
{
    public async Task<Result<IReadOnlyList<MeetingResponse>>> Handle(ListMeetingsQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<IReadOnlyList<MeetingResponse>>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        var meetings = await store.ListAsync(organizationId, userId, cancellationToken);
        return Result<IReadOnlyList<MeetingResponse>>.Success(meetings.Select(MeetingResponse.FromMeeting).ToArray());
    }
}
