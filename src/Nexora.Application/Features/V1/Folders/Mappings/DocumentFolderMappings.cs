using Nexora.Application.Features.V1.Folders.Model;
using Nexora.Domain.Documents;

namespace Nexora.Application.Features.V1.Folders.Mappings;

internal static class DocumentFolderMappings
{
    public static FolderDto ToDto(this DocumentFolder folder) =>
        new(folder.Id, folder.DepartmentId, folder.ParentFolderId, folder.Name, folder.OwnerUserId);

    public static FolderGrantDto ToDto(this DocumentFolderAccessGrant grant) =>
        new(grant.Id, grant.FolderId, grant.DepartmentId, grant.UserId, grant.AccessLevel, grant.ValidFrom, grant.ExpiresAt);
}
