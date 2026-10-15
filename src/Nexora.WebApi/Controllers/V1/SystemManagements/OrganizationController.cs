using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Features.V1.SystemManagements.Organization.Command;
using Nexora.Application.Features.V1.SystemManagements.Organization.Model;
using Nexora.Application.Features.V1.SystemManagements.Organization.Query;
using Nexora.WebApi.Middleware;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Route("api/organizations")]
public sealed class OrganizationController(ISender sender) : ControllerBase
{
    [HttpGet]
    [RequireApiFunction("ORGANIZATION.READ")]
    public async Task<IActionResult> GetOrganizations(CancellationToken cancellationToken) => this.ToSuccessResponse(await sender.Send(new GetOrganizationsQuery(), cancellationToken), "Lấy danh sách Organization thành công.");

    [HttpGet("current")]
    [RequireApiFunction("ORGANIZATION.READ", "USER.UPDATE")]
    public async Task<IActionResult> GetCurrentOrganization(CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetCurrentOrganizationQuery(), cancellationToken));

    [HttpGet("{organizationId:guid}")]
    [RequireApiFunction("ORGANIZATION.READ.BY_ID")]
    public async Task<IActionResult> GetOrganization(Guid organizationId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetOrganizationQuery(organizationId), cancellationToken));

    [HttpPost]
    [RequireApiFunction("ORGANIZATION.CREATE")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new CreateOrganizationCommand(request.Slug, request.Name, request.DataResidency), cancellationToken));

    [HttpPost("{organizationId:guid}/suspend")]
    [RequireApiFunction("ORGANIZATION.SUSPEND")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> SuspendOrganization(Guid organizationId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new SuspendOrganizationCommand(organizationId), cancellationToken));

    [HttpPost("{organizationId:guid}/activate")]
    [RequireApiFunction("ORGANIZATION.ACTIVATE")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ActivateOrganization(Guid organizationId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ActivateOrganizationCommand(organizationId), cancellationToken));

    [HttpPost("{organizationId:guid}/archive")]
    [RequireApiFunction("ORGANIZATION.ARCHIVE")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ArchiveOrganization(Guid organizationId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ArchiveOrganizationCommand(organizationId), cancellationToken));

    [HttpPut("{organizationId:guid}")]
    [RequireApiFunction("ORGANIZATION.UPDATE")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateOrganization(Guid organizationId, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new UpdateOrganizationCommand(organizationId, request.Name, request.DataResidency), cancellationToken));
}
