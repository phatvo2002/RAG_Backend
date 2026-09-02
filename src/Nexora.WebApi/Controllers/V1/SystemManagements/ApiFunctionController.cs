using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Command;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[Route("api/api-functions")]
public sealed class ApiFunctionController(ISender sender) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetApiFunctions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        this.ToSuccessResponse(
            await sender.Send(new GetApiFunctionsQuery(page, pageSize), cancellationToken),
            "Lấy danh sách ApiFunction thành công.");

    [HttpGet("{apiFunctionId:guid}")]
    public async Task<IActionResult> GetApiFunction(Guid apiFunctionId, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new GetApiFunctionQuery(apiFunctionId), cancellationToken));

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateApiFunction([FromBody] CreateApiFunctionRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(
            await sender.Send(
                new CreateApiFunctionCommand(
                    request.Code,
                    request.Module,
                    request.Name,
                    request.Description,
                    request.HttpMethod,
                    request.RouteTemplate,
                    request.ActionType,
                    request.RiskLevel,
                    request.RequiresConfirmation),
                cancellationToken));

    [HttpPost("{apiFunctionId:guid}/deactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeactivateApiFunction(Guid apiFunctionId, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new DeactivateApiFunctionCommand(apiFunctionId), cancellationToken));

    [HttpPost("{apiFunctionId:guid}/reactivate")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ReactivateApiFunction(Guid apiFunctionId, CancellationToken cancellationToken) =>
        this.ToActionResult(await sender.Send(new ReactivateApiFunctionCommand(apiFunctionId), cancellationToken));

    [HttpPut("{apiFunctionId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateApiFunction(
        Guid apiFunctionId,
        [FromBody] UpdateApiFunctionRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(
            await sender.Send(
                new UpdateApiFunctionCommand(
                    apiFunctionId,
                    request.Module,
                    request.Name,
                    request.Description,
                    request.HttpMethod,
                    request.RouteTemplate,
                    request.ActionType,
                    request.RiskLevel,
                    request.RequiresConfirmation),
                cancellationToken));
}
