namespace Nexora.Application.Common.Abstractions;

public interface ITenantContext
{
    Guid? OrganizationId { get; }
}
