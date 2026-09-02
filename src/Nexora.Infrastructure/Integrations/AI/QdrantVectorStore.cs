using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Infrastructure.Options;

namespace Nexora.Infrastructure.Rag;

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

    private sealed record CreateCollectionRequest([property: JsonPropertyName("vectors")] VectorConfiguration Vectors);
    private sealed record VectorConfiguration([property: JsonPropertyName("size")] int Size, [property: JsonPropertyName("distance")] string Distance);
    private sealed record UpsertRequest([property: JsonPropertyName("points")] QdrantPoint[] Points);
    private sealed record QdrantPoint([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("vector")] float[] Vector, [property: JsonPropertyName("payload")] Dictionary<string, object> Payload);
}
