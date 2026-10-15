using System.Text;
using System.Text.Json.Serialization;

namespace Nexora.Application.Features.V1.Rag.Retrieval.Model;

public sealed record RetrievalResponse(
    IReadOnlyList<RetrievedChunkResponse> Results,
    string AugmentedContext,
    string PromptVersion)
{
    [JsonIgnore]
    public bool DepartmentAccessDenied { get; init; }
}

public sealed record RetrievedChunkResponse(
    Guid DocumentId,
    Guid ChunkId,
    string FileName,
    string Excerpt,
    float Score,
    int? PageNo,
    int? CharStart,
    int? CharEnd);

public sealed record RagContextPassage(
    string FileName,
    string? DepartmentName,
    int? PageNo,
    int? CharStart,
    int? CharEnd,
    string Text);

public static class RagContextComposer
{
    public const string PromptVersion = "rag-context-v3";

    public static string ComposeWithoutSources(string question) =>
        $"User question:\n{question.Trim()}\n\nNo authorized source passages were retrieved.";

    public static string Compose(string organizationName, string question, IReadOnlyList<RagContextPassage> passages)
    {
        if (passages.Count == 0)
        {
            return string.Empty;
        }

        var context = new StringBuilder()
            .AppendLine($"RAG context template: {PromptVersion}")
            .AppendLine($"Company: {organizationName}")
            .AppendLine()
            .AppendLine("User question:")
            .AppendLine(question.Trim())
            .AppendLine()
            .AppendLine("Retrieved source passages, ordered by relevance:")
            .AppendLine();

        for (var index = 0; index < passages.Count; index++)
        {
            var passage = passages[index];
            context.AppendLine($"[Source {index + 1}]")
                .AppendLine($"File: {passage.FileName}");

            if (!string.IsNullOrWhiteSpace(passage.DepartmentName))
            {
                context.AppendLine($"Department: {passage.DepartmentName}");
            }

            if (passage.PageNo.HasValue)
            {
                context.AppendLine($"Page: {passage.PageNo.Value}");
            }

            if (passage.CharStart.HasValue)
            {
                context.AppendLine($"Character range: {passage.CharStart.Value}–{passage.CharEnd?.ToString() ?? "unknown"}");
            }

            context.AppendLine("Untrusted passage text:")
                .AppendLine(passage.Text)
                .AppendLine();
        }

        return context.ToString().TrimEnd();
    }
}
