using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Documents.Command;

public sealed record DeleteDocumentCommand(Guid DocumentId) : IRequest<Result>;

public sealed class DeleteDocumentCommandHandler(
    IDocumentDeletionService deletionService,
    IUserContext userContext) : IRequestHandler<DeleteDocumentCommand, Result>
{
    public Task<Result> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired)));
        }

        return deletionService.DeleteAsync(userContext.UserId.Value, request.DocumentId, cancellationToken);
    }
}
