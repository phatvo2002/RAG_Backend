namespace Nexora.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "nexora-documents";
}

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";
    public string Endpoint { get; set; } = "http://localhost:6333";
    public string ApiKey { get; set; } = string.Empty;
    public string CollectionName { get; set; } = "nexora_documents";
    public int EmbeddingDimension { get; set; } = 1536;
    public string Distance { get; set; } = "Cosine";
}

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-3-small";
    public int Dimension { get; set; } = 1536;
}

public sealed class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";
    public int ChunkSizeCharacters { get; set; } = 1200;
    public int ChunkOverlapCharacters { get; set; } = 200;
    public string PdfToTextPath { get; set; } = "pdftotext";
    public string OcrmyPdfPath { get; set; } = "ocrmypdf";
    public string OcrLanguage { get; set; } = "vie+eng";
}
