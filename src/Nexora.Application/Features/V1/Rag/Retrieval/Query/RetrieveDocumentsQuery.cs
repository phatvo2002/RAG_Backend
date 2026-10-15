using Nexora.Application.Common.Abstractions.Documents;
using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;

namespace Nexora.Application.Features.V1.Rag.Retrieval.Query;

public sealed record RetrieveDocumentsQuery(string Query) : IRequest<RetrievalResponse>;

public sealed class RetrieveDocumentsQueryHandler(
    IDefaultOrganizationProvider organizationProvider,
    IUserContext userContext,
    IDocumentProcessingStore processingStore,
    IDocumentRetrievalStore retrievalStore,
    IEmbeddingProvider embeddingProvider,
    IVectorStore vectorStore,
    IObjectStorage objectStorage,
    IClock clock,
    IDepartmentScopeOptions scopeOptions) : IRequestHandler<RetrieveDocumentsQuery, RetrievalResponse>
{
    private const int ResultLimit = 5;
    private const int ExcerptLength = 600;

    public async Task<RetrievalResponse> Handle(RetrieveDocumentsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue || !userContext.UserId.HasValue)
        {
            return EmptyResponse();
        }

        var organizationName = await retrievalStore.GetOrganizationNameAsync(organizationId.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(organizationName))
        {
            return EmptyResponse();
        }

        var index = await processingStore.GetIndexAsync(organizationId.Value, "default", false, cancellationToken);
        if (index is null || !string.Equals(index.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return EmptyResponse();
        }

        var indexedDocuments = await retrievalStore.GetIndexedDocumentsAsync(organizationId.Value, cancellationToken);
        if (indexedDocuments.Count == 0)
        {
            return EmptyResponse();
        }

        var allowedDocumentIds = await retrievalStore.GetAccessibleIndexedDocumentIdsAsync(
            organizationId.Value,
            userContext.UserId.Value,
            clock.UtcNow,
            cancellationToken);

        var vectors = await embeddingProvider.EmbedAsync([request.Query.Trim()], cancellationToken);
        if (vectors.Count != 1 || vectors[0].Length != index.EmbeddingDimension)
        {
            throw new InvalidOperationException("Query embedding does not match the active RAG index dimension.");
        }

        var topCompanyMatch = await vectorStore.SearchAsync(
            index.CollectionName,
            vectors[0],
            indexedDocuments.Select(document => document.DocumentId).Distinct().ToArray(),
            1,
            cancellationToken);
        if (DepartmentScopeGate.IsDenied(
                topCompanyMatch.FirstOrDefault(),
                scopeOptions.DepartmentMatchThreshold,
                allowedDocumentIds.ToHashSet(),
                indexedDocuments.Where(document => document.IsDepartmentScoped).Select(document => document.DocumentId).ToHashSet()))
        {
            return DepartmentAccessDeniedResponse();
        }

        if (allowedDocumentIds.Count == 0)
        {
            return EmptyResponse();
        }

        var matches = await vectorStore.SearchAsync(
            index.CollectionName,
            vectors[0],
            allowedDocumentIds,
            ResultLimit,
            cancellationToken);
        if (matches.Count == 0)
        {
            return EmptyResponse();
        }

        var sources = await retrievalStore.GetAccessibleChunkSourcesAsync(
            organizationId.Value,
            userContext.UserId.Value,
            clock.UtcNow,
            matches.Select(match => match.ChunkId).Distinct().ToArray(),
            cancellationToken);
        var sourceByChunkId = sources.ToDictionary(source => source.ChunkId);
        var results = new List<RetrievedChunkResponse>(ResultLimit);
        var contextPassages = new List<RagContextPassage>(ResultLimit);

        foreach (var match in matches)
        {
            if (!sourceByChunkId.TryGetValue(match.ChunkId, out var source) || source.DocumentId != match.DocumentId)
            {
                continue;
            }

            await using var content = await objectStorage.GetAsync(source.StorageBucket, source.ContentObjectKey, cancellationToken);
            using var reader = new StreamReader(content);
            var text = (await reader.ReadToEndAsync(cancellationToken)).Trim();
            var excerpt = text.Length <= ExcerptLength ? text : $"{text[..ExcerptLength]}…";
            contextPassages.Add(new RagContextPassage(
                source.FileName,
                source.DepartmentName,
                source.PageNo,
                source.CharStart,
                source.CharEnd,
                excerpt));
            results.Add(new RetrievedChunkResponse(
                source.DocumentId,
                source.ChunkId,
                source.FileName,
                excerpt,
                match.Score,
                source.PageNo,
                source.CharStart,
                source.CharEnd));
        }

        return new RetrievalResponse(results,
            RagContextComposer.Compose(organizationName, request.Query, contextPassages),
            RagContextComposer.PromptVersion);
    }

    private static RetrievalResponse EmptyResponse() =>
        new(Array.Empty<RetrievedChunkResponse>(), string.Empty, RagContextComposer.PromptVersion);

    private static RetrievalResponse DepartmentAccessDeniedResponse() =>
        new(Array.Empty<RetrievedChunkResponse>(), string.Empty, RagContextComposer.PromptVersion)
        {
            DepartmentAccessDenied = true
        };
}
