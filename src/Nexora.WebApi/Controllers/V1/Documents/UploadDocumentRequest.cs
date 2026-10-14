namespace Nexora.WebApi.Controllers.V1.Documents;

public sealed record UploadDocumentRequest
{
    public Guid FolderId { get; set; }
    public IFormFile File { get; set; } = null!;
}
