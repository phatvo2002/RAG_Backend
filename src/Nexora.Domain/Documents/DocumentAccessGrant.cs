using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Documents;

public class DocumentAccessGrant : CreatedConcurrencyEntityBase
{
    protected DocumentAccessGrant()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid DocumentId { get; private set; }

    public string ScopeType { get; set; } = string.Empty;

    public Guid? PrincipalDepartmentId { get; private set; }

    public Guid? PrincipalProjectId { get; private set; }

    public Guid? PrincipalUserId { get; private set; }

    public string AccessLevel { get; set; } = string.Empty;

    public Guid GrantedByUserId { get; private set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? RevokedByUserId { get; private set; }

}
