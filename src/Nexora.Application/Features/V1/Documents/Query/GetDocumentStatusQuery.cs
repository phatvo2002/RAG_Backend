using Nexora.Application.Common.Abstractions.Documents;
using MediatR;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Results;
using Nexora.Application.Common.Constants;
using Nexora.Application.Features.V1.Documents.Model;

namespace Nexora.Application.Features.V1.Documents.Query;

public sealed record GetDocumentStatusQuery(Guid DocumentId) : IRequest<Result<DocumentStatusResponse>>;

public sealed class GetDocumentStatusQueryHandler(IDocumentProcessingStore store, Nexora.Application.Common.Abstractions.IUserContext userContext) : IRequestHandler<GetDocumentStatusQuery, Result<DocumentStatusResponse>>
{
    public async Task<Result<DocumentStatusResponse>> Handle(GetDocumentStatusQuery request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<DocumentStatusResponse>.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var document = await store.GetDocumentAsync(userContext.UserId.Value, request.DocumentId, false, cancellationToken);
        if (document is null || !document.CurrentVersionId.HasValue)
            return Result<DocumentStatusResponse>.Failure(Error.NotFound(CommonMessages.Document.NotFound));
        var version = await store.GetVersionAsync(document.CurrentVersionId.Value, false, cancellationToken);
        if (version is null)
            return Result<DocumentStatusResponse>.Failure(Error.NotFound(CommonMessages.Document.NotFound));
        var chunks = await store.GetChunksAsync(version.Id, false, cancellationToken);
        return Result<DocumentStatusResponse>.Success(new DocumentStatusResponse(document.Id, version.Id, version.OriginalFileName, version.ProcessingStatus, version.FailureCode, version.CreatedAt, version.ReadyAt, chunks.Count));
    }
}
