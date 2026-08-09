namespace Nexora.Application.Features.V1.SystemManagements.Organization.Model;

public sealed record OrganizationResponse(Guid OrganizationId, string Slug, string Name, string Status, string? DataResidency);

public sealed record CreateOrganizationRequest(string Slug, string Name, string? DataResidency = null);

public sealed record UpdateOrganizationRequest(string Name, string? DataResidency = null);
