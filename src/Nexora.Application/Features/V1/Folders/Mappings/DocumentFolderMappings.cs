using Nexora.Application.Features.V1.Folders.Model;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Domain.Documents;

namespace Nexora.Application.Features.V1.Folders.Mappings;

internal static class DocumentFolderMappings
{
    public static FolderDto ToDto(this DocumentFolder folder) =>
        new(folder.Id, folder.DepartmentId, folder.ParentFolderId, folder.Name, folder.OwnerUserId);

    public static FolderOverviewDto ToOverviewDto(this DocumentFolder folder, FolderDocumentSummary? summary) =>
        new(
            folder.Id,
            folder.DepartmentId,
            folder.ParentFolderId,
            folder.Name,
            folder.OwnerUserId,
            folder.CreatedAt,
            folder.UpdatedAt,
            summary?.DocumentCount ?? 0,
            summary?.TotalSizeBytes ?? 0,
            summary?.IndexedCount ?? 0,
            summary?.ProcessingCount ?? 0,
            summary?.FailedCount ?? 0,
            summary?.LatestDocumentUpdatedAt);

    public static FolderGrantDto ToDto(this DocumentFolderAccessGrant grant) =>
        new(grant.Id, grant.FolderId, grant.DepartmentId, grant.UserId, grant.AccessLevel, grant.ValidFrom, grant.ExpiresAt);
}
