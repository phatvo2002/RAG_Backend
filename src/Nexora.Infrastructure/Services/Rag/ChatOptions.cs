using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Rag;

public sealed class ChatOptions : IDepartmentScopeOptions
{
    public const string SectionName = "Chat";
    public string EncryptionKeyBase64 { get; set; } = string.Empty;
    public string Model { get; set; } = "qwen3.5:4b";
    public int ContextWindowTokens { get; set; } = 8192;
    public int MaxOutputTokens { get; set; } = 768;
    public int EstimatedRetrievedContextTokens { get; set; } = 2048;
    public float DepartmentMatchThreshold { get; set; } = 0.70f;
    public float MinimumRagScore { get; set; } = 0.65f;
    public int EvidenceAssessmentTimeoutSeconds { get; set; } = 60;
}
