using Nexora.Application.Common.Abstractions.Account;

namespace Nexora.Infrastructure.Services.Authentication;

public sealed class ProviderKeyOptions : IProviderEndpointSettings
{
    public const string SectionName = "ProviderKeys";
    public string EncryptionKeyBase64 { get; set; } = string.Empty;
    public Dictionary<string, string> ClaudeEndpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["official"] = "https://api.anthropic.com/v1"
    };

    IReadOnlyDictionary<string, string> IProviderEndpointSettings.ClaudeEndpoints => ClaudeEndpoints;
}
