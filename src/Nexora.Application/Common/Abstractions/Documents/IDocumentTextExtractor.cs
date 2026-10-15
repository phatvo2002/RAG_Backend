namespace Nexora.Application.Common.Abstractions.Documents;

public sealed record ExtractedDocumentText(string Text, string ParserName, string ParserVersion);

public interface IDocumentTextExtractor
{
    Task<ExtractedDocumentText> ExtractAsync(string fileName, Stream content, CancellationToken cancellationToken);
}
