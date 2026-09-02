using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Documents.Model;

namespace Nexora.Application.Features.V1.Documents.Command;

public sealed record UploadDocumentCommand(Guid FolderId, string FileName, string ContentType, long SizeBytes, Stream Content) : IRequest<Result<DocumentUploadResponse>>;

public sealed class UploadDocumentCommandHandler(IDocumentIngestionService ingestionService, IUserContext userContext) : IRequestHandler<UploadDocumentCommand, Result<DocumentUploadResponse>>
{
    public async Task<Result<DocumentUploadResponse>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<DocumentUploadResponse>.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var result = await ingestionService.UploadAsync(userContext.UserId.Value, request.FolderId, request.FileName, request.ContentType, request.SizeBytes, request.Content, cancellationToken);
        return result.IsSuccess
            ? Result<DocumentUploadResponse>.Success(new DocumentUploadResponse(result.Value!.DocumentId, result.Value.VersionId, result.Value.ProcessingStatus))
            : Result<DocumentUploadResponse>.Failure(result.Error!);
    }
}
