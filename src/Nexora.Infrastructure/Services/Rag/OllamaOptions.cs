namespace Nexora.Infrastructure.Services.Rag;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";
    public string Url { get; set; } = "http://localhost:11434/";
}
