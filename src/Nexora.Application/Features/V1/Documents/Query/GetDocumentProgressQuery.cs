using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Features.V1.Documents.Model;

namespace Nexora.Application.Features.V1.Documents.Query;

public sealed record GetDocumentProgressQuery : IRequest<IReadOnlyList<DocumentProgressResponse>>;

public sealed class GetDocumentProgressQueryHandler(
    IDocumentProcessingStore store,
    IDefaultOrganizationProvider organizationProvider,
    IUserContext userContext,
    IClock clock,
    IDocumentIndexSettings indexSettings) : IRequestHandler<GetDocumentProgressQuery, IReadOnlyList<DocumentProgressResponse>>
{
    public async Task<IReadOnlyList<DocumentProgressResponse>> Handle(GetDocumentProgressQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue || !userContext.UserId.HasValue) return Array.Empty<DocumentProgressResponse>();
        var records = await store.GetAccessibleProgressAsync(organizationId.Value, userContext.UserId.Value,
            clock.UtcNow, indexSettings.IndexName, cancellationToken);
        return records.Select(record => new DocumentProgressResponse(record.DocumentId, record.VersionId, record.FileName,
            record.ProcessingStatus, indexSettings.ActiveProvider, record.IndexStatus, record.IndexedChunks, record.TotalChunks,
            record.FailureCode == "EMBEDDING_RATE_LIMITED",
            record.ProcessingStatus == "READY" && record.IndexedChunks < record.TotalChunks,
            record.FailureCode, record.CreatedAt, record.ReadyAt)).ToArray();
    }
}
