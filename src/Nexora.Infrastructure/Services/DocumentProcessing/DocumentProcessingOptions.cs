namespace Nexora.Infrastructure.Services.DocumentProcessing;

public sealed class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";
    public int ChunkSizeCharacters { get; set; } = 1200;
    public int ChunkOverlapCharacters { get; set; } = 200;
    public string TesseractPath { get; set; } = "tesseract";
    public string OcrLanguage { get; set; } = "vie+eng";
    public int PdfOcrDpi { get; set; } = 200;
}
