using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Rag;

public sealed class ChatOptions : IDepartmentScopeOptions
{
    public const string SectionName = "Chat";
    public string EncryptionKeyBase64 { get; set; } = string.Empty;
    public string GeminiModel { get; set; } = "gemini-3.8-flash";
    public string ClaudeModel { get; set; } = "claude-sonnet-5";
    public int MaxOutputTokens { get; set; } = 768;
    public float DepartmentMatchThreshold { get; set; } = 0.70f;
}
