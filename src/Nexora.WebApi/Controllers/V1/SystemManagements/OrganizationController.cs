using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.SystemManagements.Organization.Command;
using Nexora.Application.Features.V1.SystemManagements.Organization.Model;
using Nexora.Application.Features.V1.SystemManagements.Organization.Query;

namespace Nexora.WebApi.Controllers.V1.SystemManagements;

[ApiController]
[Route("api/organizations")]
public sealed class OrganizationController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrganizations(CancellationToken cancellationToken) => this.ToSuccessResponse(await sender.Send(new GetOrganizationsQuery(), cancellationToken), "Lấy danh sách Organization thành công.");

    [HttpPost]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new CreateOrganizationCommand(request.Slug, request.Name, request.DataResidency), cancellationToken));

    [HttpGet("{organizationId:guid}")]
    public async Task<IActionResult> GetOrganization(Guid organizationId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new GetOrganizationQuery(organizationId), cancellationToken));

    [HttpPut("{organizationId:guid}")]
    public async Task<IActionResult> UpdateOrganization(Guid organizationId, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new UpdateOrganizationCommand(organizationId, request.Name, request.DataResidency), cancellationToken));

    [HttpPost("{organizationId:guid}/suspend")]
    public async Task<IActionResult> SuspendOrganization(Guid organizationId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new SuspendOrganizationCommand(organizationId), cancellationToken));

    [HttpPost("{organizationId:guid}/activate")]
    public async Task<IActionResult> ActivateOrganization(Guid organizationId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ActivateOrganizationCommand(organizationId), cancellationToken));

    [HttpPost("{organizationId:guid}/archive")]
    public async Task<IActionResult> ArchiveOrganization(Guid organizationId, CancellationToken cancellationToken) => this.ToActionResult(await sender.Send(new ArchiveOrganizationCommand(organizationId), cancellationToken));
}
