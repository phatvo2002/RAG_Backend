using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Integrations.VectorStores;

internal sealed class QdrantVectorStore(HttpClient httpClient) : IVectorStore
{
    public async Task EnsureCollectionAsync(string collectionName, int dimension, string distance, CancellationToken cancellationToken)
    {
        using var getResponse = await httpClient.GetAsync($"collections/{Uri.EscapeDataString(collectionName)}", cancellationToken);
        if (getResponse.IsSuccessStatusCode)
        {
            return;
        }

        if (getResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            getResponse.EnsureSuccessStatusCode();
        }

        using var response = await httpClient.PutAsJsonAsync($"collections/{Uri.EscapeDataString(collectionName)}", new CreateCollectionRequest(new VectorConfiguration(dimension, distance)), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpsertAsync(string collectionName, IReadOnlyCollection<VectorPoint> points, CancellationToken cancellationToken)
    {
        if (points.Count == 0)
        {
            return;
        }

        var request = new UpsertRequest(points.Select(point => new QdrantPoint(point.Id, point.Vector, new Dictionary<string, object>
        {
            ["documentId"] = point.DocumentId,
            ["versionId"] = point.VersionId,
            ["chunkId"] = point.ChunkId,
            ["contentHash"] = point.ContentHash,
            ["fileName"] = point.FileName
        })).ToArray());
        using var response = await httpClient.PutAsJsonAsync($"collections/{Uri.EscapeDataString(collectionName)}/points?wait=true", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        float[] vector,
        IReadOnlyCollection<Guid> allowedDocumentIds,
        int limit,
        CancellationToken cancellationToken)
    {
        if (allowedDocumentIds.Count == 0 || limit <= 0)
        {
            return Array.Empty<VectorSearchResult>();
        }

        var request = new QueryPointsRequest(
            vector,
            new QueryFilter([new PayloadCondition("documentId", new MatchAny(allowedDocumentIds.Select(id => id.ToString("D")).ToArray()))]),
            limit,
            true);
        using var response = await httpClient.PostAsJsonAsync(
            $"collections/{Uri.EscapeDataString(collectionName)}/points/query",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<QueryPointsResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Qdrant returned an empty vector search response.");

        return payload.Result.Points
            .Select(point =>
            {
                var documentId = point.Payload.DocumentId ?? throw new InvalidOperationException("Qdrant result is missing documentId.");
                var chunkId = point.Payload.ChunkId is { } payloadChunkId
                    ? Guid.Parse(payloadChunkId)
                    : Guid.Parse(point.Id);
                return new VectorSearchResult(Guid.Parse(documentId), chunkId, point.Score);
            })
            .Where(result => allowedDocumentIds.Contains(result.DocumentId))
            .ToArray();
    }

    public async Task DeleteAsync(string collectionName, IReadOnlyCollection<string> pointIds, CancellationToken cancellationToken)
    {
        if (pointIds.Count == 0)
        {
            return;
        }

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"collections/{Uri.EscapeDataString(collectionName)}/points/delete?wait=true")
        {
            Content = JsonContent.Create(new DeletePointsRequest(pointIds.ToArray()))
        };
        using (request)
        using (var response = await httpClient.SendAsync(request, cancellationToken))
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                using var collectionResponse = await httpClient.GetAsync(
                    $"collections/{Uri.EscapeDataString(collectionName)}", cancellationToken);
                if (collectionResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return;
                collectionResponse.EnsureSuccessStatusCode();
            }
            response.EnsureSuccessStatusCode();
        }
    }

    private sealed record CreateCollectionRequest([property: JsonPropertyName("vectors")] VectorConfiguration Vectors);
    private sealed record VectorConfiguration([property: JsonPropertyName("size")] int Size, [property: JsonPropertyName("distance")] string Distance);
    private sealed record UpsertRequest([property: JsonPropertyName("points")] QdrantPoint[] Points);
    private sealed record DeletePointsRequest([property: JsonPropertyName("points")] string[] Points);
    private sealed record QdrantPoint([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("vector")] float[] Vector, [property: JsonPropertyName("payload")] Dictionary<string, object> Payload);
    private sealed record QueryPointsRequest(
        [property: JsonPropertyName("query")] float[] Query,
        [property: JsonPropertyName("filter")] QueryFilter Filter,
        [property: JsonPropertyName("limit")] int Limit,
        [property: JsonPropertyName("with_payload")] bool WithPayload);
    private sealed record QueryFilter([property: JsonPropertyName("must")] PayloadCondition[] Must);
    private sealed record PayloadCondition([property: JsonPropertyName("key")] string Key, [property: JsonPropertyName("match")] MatchAny Match);
    private sealed record MatchAny([property: JsonPropertyName("any")] string[] Any);
    private sealed record QueryPointsResponse([property: JsonPropertyName("result")] QueryResult Result);
    private sealed record QueryResult([property: JsonPropertyName("points")] QueryPoint[] Points);
    private sealed record QueryPoint(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("score")] float Score,
        [property: JsonPropertyName("payload")] QueryPointPayload Payload);
    private sealed record QueryPointPayload(
        [property: JsonPropertyName("documentId")] string? DocumentId,
        [property: JsonPropertyName("chunkId")] string? ChunkId);
}
