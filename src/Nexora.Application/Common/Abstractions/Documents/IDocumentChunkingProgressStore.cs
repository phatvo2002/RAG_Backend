namespace Nexora.Application.Common.Abstractions.Documents;

public interface IDocumentChunkingProgressStore
{
    DocumentChunkingProgress? Get(Guid versionId);
    void Report(Guid versionId, DocumentChunkingProgress progress);
    void Clear(Guid versionId);
}
