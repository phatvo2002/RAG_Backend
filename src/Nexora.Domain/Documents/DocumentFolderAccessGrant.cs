using Nexora.Domain.Common;
using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Documents;

public sealed class DocumentFolderAccessGrant : CreatedConcurrencyEntityBase
{
    private DocumentFolderAccessGrant() { }

    public Guid OrganizationId { get; private set; }
    public Guid FolderId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? UserId { get; private set; }
    public string AccessLevel { get; private set; } = string.Empty;
    public Guid GrantedByUserId { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }

    public static DocumentFolderAccessGrant Create(Guid organizationId, Guid folderId, Guid? departmentId, Guid? userId, string accessLevel, Guid grantedByUserId, DateTimeOffset validFrom, DateTimeOffset? expiresAt = null)
    {
        if ((departmentId.HasValue ? 1 : 0) + (userId.HasValue ? 1 : 0) != 1) throw new ArgumentException("Exactly one grant principal is required.");
        if (expiresAt.HasValue && expiresAt.Value <= validFrom) throw new ArgumentException("Grant expiry must be after valid-from.");
        return new DocumentFolderAccessGrant
        {
            OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId)), FolderId = DomainGuard.RequiredId(folderId, nameof(folderId)),
            DepartmentId = departmentId, UserId = userId, AccessLevel = DomainGuard.RequiredText(accessLevel, nameof(accessLevel), 20),
            GrantedByUserId = DomainGuard.RequiredId(grantedByUserId, nameof(grantedByUserId)), ValidFrom = validFrom, ExpiresAt = expiresAt
        };
    }

    public void Revoke(Guid userId, DateTimeOffset revokedAt) { RevokedByUserId = DomainGuard.RequiredId(userId, nameof(userId)); RevokedAt = revokedAt; }
}
