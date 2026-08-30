using MediatR;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Results;
using Nexora.Application.Common.Constants;
using Nexora.Application.Features.V1.Documents.Model;

namespace Nexora.Application.Features.V1.Documents.Query;

public sealed record GetDocumentStatusQuery(Guid DocumentId) : IRequest<Result<DocumentStatusResponse>>;

public sealed class GetDocumentStatusQueryHandler(IDocumentIngestionService ingestionService, Nexora.Application.Common.Abstractions.IUserContext userContext) : IRequestHandler<GetDocumentStatusQuery, Result<DocumentStatusResponse>>
{
    public async Task<Result<DocumentStatusResponse>> Handle(GetDocumentStatusQuery request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<DocumentStatusResponse>.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var status = await ingestionService.GetStatusAsync(userContext.UserId.Value, request.DocumentId, cancellationToken);
        return status is null
            ? Result<DocumentStatusResponse>.Failure(Error.NotFound(CommonMessages.Document.NotFound))
            : Result<DocumentStatusResponse>.Success(new DocumentStatusResponse(status.DocumentId, status.VersionId, status.FileName, status.ProcessingStatus, status.FailureCode, status.CreatedAt, status.ReadyAt, status.ChunkCount));
    }
}
