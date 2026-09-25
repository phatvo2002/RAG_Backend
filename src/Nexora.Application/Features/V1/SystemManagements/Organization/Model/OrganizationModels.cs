namespace Nexora.Application.Features.V1.SystemManagements.Organization.Model;

public sealed record OrganizationResponse
{
    public OrganizationResponse()
    {
    }

    public OrganizationResponse(Guid organizationId, string slug, string name, string status, string? dataResidency)
    {
        OrganizationId = organizationId;
        Slug = slug;
        Name = name;
        Status = status;
        DataResidency = dataResidency;
    }

    public Guid OrganizationId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DataResidency { get; set; }
}

public sealed record CreateOrganizationRequest
{
    public CreateOrganizationRequest()
    {
    }

    public CreateOrganizationRequest(string slug, string name, string? dataResidency = null)
    {
        Slug = slug;
        Name = name;
        DataResidency = dataResidency;
    }

    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DataResidency { get; set; }
}

public sealed record UpdateOrganizationRequest
{
    public UpdateOrganizationRequest()
    {
    }

    public UpdateOrganizationRequest(string name, string? dataResidency = null)
    {
        Name = name;
        DataResidency = dataResidency;
    }

    public string Name { get; set; } = string.Empty;
    public string? DataResidency { get; set; }
}
