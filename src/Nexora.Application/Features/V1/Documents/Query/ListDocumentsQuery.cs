using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Documents.Model;

namespace Nexora.Application.Features.V1.Documents.Query;

public sealed record ListDocumentsQuery(
    Guid? FolderId = null,
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    string? FileType = null,
    string? ProcessingStatus = null,
    Guid? DepartmentId = null) : IRequest<PagedResult<DocumentListItemResponse>>;

public sealed class ListDocumentsQueryHandler(
    IDocumentRepository documentRepository,
    IDefaultOrganizationProvider organizationProvider,
    IUserContext userContext,
    IClock clock) : IRequestHandler<ListDocumentsQuery, PagedResult<DocumentListItemResponse>>
{
    public async Task<PagedResult<DocumentListItemResponse>> Handle(ListDocumentsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 25 : request.PageSize;
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue || !userContext.UserId.HasValue)
        {
            return new PagedResult<DocumentListItemResponse>(Array.Empty<DocumentListItemResponse>(), page, pageSize, 0);
        }

        var result = await documentRepository.ListAsync(
            new DocumentListFilter(
                organizationId.Value,
                userContext.UserId.Value,
                request.FolderId,
                request.DepartmentId,
                request.Search,
                request.FileType,
                request.ProcessingStatus,
                clock.UtcNow,
                page,
                pageSize),
            cancellationToken);

        var items = result.Items
            .Select(item => new DocumentListItemResponse(
                item.DocumentId,
                item.FolderId,
                item.FolderName,
                item.DepartmentId,
                item.Title,
                item.OriginalFileName,
                item.MimeType,
                item.SizeBytes,
                item.DocumentStatus,
                item.ProcessingStatus,
                item.FailureCode,
                item.CreatedAt,
                item.UpdatedAt,
                item.ReadyAt))
            .ToArray();

        return new PagedResult<DocumentListItemResponse>(items, result.Page, result.PageSize, result.TotalCount);
    }
}
