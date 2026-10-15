using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Meetings.Query;

public sealed record FindEligibleMeetingUsersQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<MeetingUserOption>>>;

public sealed class FindEligibleMeetingUsersQueryHandler(IMeetingStore store, IUserContext user, ITenantContext tenant)
    : IRequestHandler<FindEligibleMeetingUsersQuery, Result<PagedResult<MeetingUserOption>>>
{
    public async Task<Result<PagedResult<MeetingUserOption>>> Handle(FindEligibleMeetingUsersQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId || tenant.OrganizationId is not { } organizationId)
            return Result<PagedResult<MeetingUserOption>>.Failure(Error.Unauthorized("Vui lòng đăng nhập."));
        if (!await store.AreActiveMembersAsync(organizationId, new[] { userId }, cancellationToken))
            return Result<PagedResult<MeetingUserOption>>.Failure(Error.Forbidden("Không thuộc tổ chức."));
        var result = await store.FindEligibleUsersAsync(organizationId, request.Search?.Trim(), request.Page, request.PageSize, cancellationToken);
        return Result<PagedResult<MeetingUserOption>>.Success(result);
    }
}
