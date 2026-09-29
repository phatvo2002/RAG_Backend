namespace Nexora.Infrastructure.Options;

public sealed class ProviderKeyOptions
{
    public const string SectionName = "ProviderKeys";
    public string EncryptionKeyBase64 { get; set; } = string.Empty;
    public Dictionary<string, string> ClaudeEndpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["official"] = "https://api.anthropic.com/v1"
    };
}
