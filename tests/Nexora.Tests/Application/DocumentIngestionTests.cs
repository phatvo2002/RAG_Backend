using Nexora.Application.Features.V1.Documents.Command;
using Nexora.Application.Features.V1.Documents.Validation;

namespace Nexora.Tests.Application;

public sealed class DocumentIngestionTests
{
    [Fact]
    public void UploadValidator_ShouldAcceptSupportedFileWithinLimit()
    {
        var command = new UploadDocumentCommand(Guid.NewGuid(), "handbook.pdf", "application/pdf", 1024, new MemoryStream([1, 2, 3]));

        var result = new UploadDocumentCommandValidator().Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UploadValidator_ShouldRejectUnsupportedExtension()
    {
        var command = new UploadDocumentCommand(Guid.NewGuid(), "payload.exe", "application/octet-stream", 1024, new MemoryStream([1]));

        var result = new UploadDocumentCommandValidator().Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UploadValidator_ShouldRejectFilesLargerThan25Mb()
    {
        var command = new UploadDocumentCommand(Guid.NewGuid(), "large.txt", "text/plain", 25 * 1024 * 1024 + 1, new MemoryStream([1]));

        var result = new UploadDocumentCommandValidator().Validate(command);

        Assert.False(result.IsValid);
    }
}
