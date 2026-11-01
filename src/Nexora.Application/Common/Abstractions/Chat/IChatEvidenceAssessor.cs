namespace Nexora.Application.Common.Abstractions.Chat;

public sealed record ChatEvidenceCandidate(Guid ChunkId, string Text);
public sealed record ChatEvidenceSelection(Guid ChunkId, string Quote, string? Subject, string? Value);
public enum ChatEvidenceMode { Supported, General, Missing, Clarify }
public sealed record ChatEvidenceAssessment(ChatEvidenceMode Mode, IReadOnlyList<ChatEvidenceSelection> Selections);

public interface IChatEvidenceAssessor
{
    Task<ChatEvidenceAssessment> AssessAsync(string question, IReadOnlyList<ChatEvidenceCandidate> candidates,
        CancellationToken cancellationToken);
}
