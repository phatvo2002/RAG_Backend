using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.ActivityLog.Model;

namespace Nexora.Application.Features.V1.SystemManagements.ActivityLog.Query;

public sealed record GetActivityLogsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<ActivityLogResponse>>;

public sealed class GetActivityLogsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetActivityLogsQuery, PagedResult<ActivityLogResponse>>
{
    public async Task<PagedResult<ActivityLogResponse>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var logs = await unitOfWork.ActivityLogRepository.PaginationAsync(
            page,
            pageSize,
            orderBy: query => query.OrderByDescending(log => log.OccurredAt),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        var items = logs.Data.Select(log => new ActivityLogResponse(
            log.Id,
            log.ActorUserId,
            log.ApiFunctionId,
            log.Module,
            log.Action,
            log.ResourceType,
            log.Result,
            log.Description,
            log.IpAddress?.ToString(),
            log.MetadataJson,
            log.OccurredAt)).ToArray();

        return new PagedResult<ActivityLogResponse>(items, page, pageSize, logs.Total);
    }
}
