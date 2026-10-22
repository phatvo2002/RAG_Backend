using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Documents.Query;

public sealed record GetDocumentChunksQuery(Guid DocumentId, int Page = 1, int PageSize = 25)
    : IRequest<Result<PagedResult<DocumentChunkView>>>;

public sealed record DocumentChunkView(
    Guid ChunkId, int Ordinal, int? PageNo, int? CharStart, int? CharEnd,
    string? LanguageCode, string Text);

public sealed class GetDocumentChunksQueryHandler(
    IDocumentChunkReadStore store,
    IDefaultOrganizationProvider organizationProvider,
    IUserContext userContext,
    IClock clock,
    IObjectStorage objectStorage)
    : IRequestHandler<GetDocumentChunksQuery, Result<PagedResult<DocumentChunkView>>>
{
    public async Task<Result<PagedResult<DocumentChunkView>>> Handle(
        GetDocumentChunksQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue || !userContext.UserId.HasValue)
            return Result<PagedResult<DocumentChunkView>>.Failure(
                Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));

        var pageSize = request.PageSize is < 1 or > 50 ? 25 : request.PageSize;
        var page = Math.Clamp(request.Page, 1, int.MaxValue / pageSize);
        var records = await store.GetAccessiblePageAsync(organizationId.Value, userContext.UserId.Value,
            clock.UtcNow, request.DocumentId, page, pageSize, cancellationToken);
        if (records is null)
            return Result<PagedResult<DocumentChunkView>>.Failure(
                Error.NotFound(CommonMessages.Document.NotFound));

        var chunks = new List<DocumentChunkView>(records.Items.Count);
        foreach (var record in records.Items)
        {
            await using var stream = await objectStorage.GetAsync(
                record.StorageBucket, record.ContentObjectKey, cancellationToken);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync(cancellationToken);
            chunks.Add(new DocumentChunkView(record.ChunkId, record.Ordinal, record.PageNo,
                record.CharStart, record.CharEnd, record.LanguageCode, text));
        }

        return Result<PagedResult<DocumentChunkView>>.Success(
            new PagedResult<DocumentChunkView>(chunks, page, pageSize, records.TotalCount));
    }
}
