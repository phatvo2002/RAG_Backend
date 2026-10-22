using System.Collections.Concurrent;
using Nexora.Application.Common.Abstractions.Documents;

namespace Nexora.Infrastructure.Services.DocumentProcessing;

internal sealed class DocumentChunkingProgressStore : IDocumentChunkingProgressStore
{
    private readonly ConcurrentDictionary<Guid, DocumentChunkingProgress> progress = new();

    public DocumentChunkingProgress? Get(Guid versionId) =>
        progress.TryGetValue(versionId, out var value) ? value : null;

    public void Report(Guid versionId, DocumentChunkingProgress value) =>
        progress.AddOrUpdate(versionId, value, (_, previous) =>
            PhaseOrder(previous.Phase) > PhaseOrder(value.Phase) ||
            (previous.Phase == value.Phase && previous.Total == value.Total && previous.Completed > value.Completed)
                ? previous : value);

    public void Clear(Guid versionId) => progress.TryRemove(versionId, out _);

    private static int PhaseOrder(string phase) => phase switch
    {
        "ANALYZING" => 0,
        "BUILDING" => 1,
        "SAVING" => 2,
        _ => -1
    };
}
