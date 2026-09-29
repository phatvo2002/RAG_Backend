using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Abstractions.Rag;

public interface IDocumentDeletionService
{
    Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken cancellationToken);
}
