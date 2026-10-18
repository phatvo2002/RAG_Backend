namespace Nexora.Infrastructure.Services.DocumentProcessing;

public sealed class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";
    public int ChunkSizeCharacters { get; set; } = 1200;
    public int ChunkOverlapCharacters { get; set; } = 200;
    public int EmbeddingBatchSize { get; set; } = 5;
    public int BatchIntervalSeconds { get; set; } = 35;
    public int RateLimitRetrySeconds { get; set; } = 60;
    public string TesseractPath { get; set; } = "tesseract";
    public string OcrLanguage { get; set; } = "vie+eng";
    public int PdfOcrDpi { get; set; } = 200;
}
