using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Documents;

namespace Nexora.Application.Common.Abstractions.Documents;

public interface IDocumentFolderRepository : IBaseRepository<DocumentFolder>
{
    Task<DocumentFolder?> GetByIdAsync(Guid folderId, bool asTracking, CancellationToken cancellationToken);

    Task<List<DocumentFolder>> GetForOrganizationAsync(Guid organizationId, Guid? departmentId, CancellationToken cancellationToken);

    Task<bool> ExistsInDepartmentAsync(Guid organizationId, Guid departmentId, Guid folderId, CancellationToken cancellationToken);

    Task<bool> HasActiveDocumentsAsync(Guid folderId, CancellationToken cancellationToken);
}

public interface IDocumentFolderAccessGrantRepository : IBaseRepository<DocumentFolderAccessGrant>
{
    Task<List<DocumentFolderAccessGrant>> GetByFolderAsync(Guid organizationId, Guid folderId, CancellationToken cancellationToken);

    Task<DocumentFolderAccessGrant?> GetByIdAsync(Guid organizationId, Guid folderId, Guid grantId, bool asTracking, CancellationToken cancellationToken);
}
