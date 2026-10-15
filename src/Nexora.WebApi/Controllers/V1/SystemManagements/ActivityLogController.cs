using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.SystemManagements.ActivityLog.Query;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[Route("api/activity-logs")]
public sealed class ActivityLogController(ISender sender) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetActivityLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        this.ToSuccessResponse(await sender.Send(new GetActivityLogsQuery(page, pageSize), cancellationToken), "Lấy danh sách ActivityLog thành công.");
}
