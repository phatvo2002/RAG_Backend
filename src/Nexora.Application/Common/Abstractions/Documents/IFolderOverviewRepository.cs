namespace Nexora.Application.Common.Abstractions.Documents;

public sealed record FolderDocumentSummary(
    Guid FolderId,
    int DocumentCount,
    long TotalSizeBytes,
    int IndexedCount,
    int ProcessingCount,
    int FailedCount,
    DateTimeOffset? LatestDocumentUpdatedAt);

public interface IFolderOverviewRepository
{
    Task<IReadOnlyList<FolderDocumentSummary>> GetFolderSummariesAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> folderIds,
        CancellationToken cancellationToken);
}
