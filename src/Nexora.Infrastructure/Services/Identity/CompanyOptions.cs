namespace Nexora.Infrastructure.Services.Identity;

public sealed class CompanyOptions
{
    public const string SectionName = "Company";

    public string DefaultOrganizationId { get; set; } = string.Empty;

    public Guid? TryGetDefaultOrganizationId() =>
        Guid.TryParse(DefaultOrganizationId, out var organizationId) ? organizationId : null;
}
