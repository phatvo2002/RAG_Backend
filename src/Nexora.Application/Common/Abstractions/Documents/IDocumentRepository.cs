using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Abstractions.Documents;

public sealed record DocumentListFilter(
    Guid OrganizationId,
    Guid UserId,
    Guid FolderId,
    Guid? DepartmentId,
    string? Search,
    string? FileType,
    string? ProcessingStatus,
    DateTimeOffset Now,
    int Page,
    int PageSize);

public sealed record DocumentListRecord(
    Guid DocumentId,
    Guid FolderId,
    string FolderName,
    Guid? DepartmentId,
    string Title,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string DocumentStatus,
    string ProcessingStatus,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ReadyAt);

public interface IDocumentRepository
{
    Task<PagedResult<DocumentListRecord>> ListAsync(DocumentListFilter filter, CancellationToken cancellationToken);
}
