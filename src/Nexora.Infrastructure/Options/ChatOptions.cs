namespace Nexora.Infrastructure.Options;

public sealed class ChatOptions
{
    public const string SectionName = "Chat";
    public string EncryptionKeyBase64 { get; set; } = string.Empty;
    public string GeminiModel { get; set; } = "gemini-3.8-flash";
    public string ClaudeModel { get; set; } = "claude-sonnet-5";
}
