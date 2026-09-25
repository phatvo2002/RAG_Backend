using FluentValidation;
using Nexora.Application.Features.V1.Documents.Command;
using Nexora.Application.Features.V1.Documents.Query;

namespace Nexora.Application.Features.V1.Documents.Validation;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly string[] AllowedExtensions = [".pdf", ".txt", ".md", ".docx"];

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500).Must(HasAllowedExtension).WithMessage("Chỉ hỗ trợ file PDF, TXT, MD hoặc DOCX.");
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(25 * 1024 * 1024);
        RuleFor(x => x.Content).NotNull();
    }

    private static bool HasAllowedExtension(string fileName) => AllowedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
}

public sealed class ListDocumentsQueryValidator : AbstractValidator<ListDocumentsQuery>
{
    private static readonly string[] AllowedFileTypes = ["pdf", "txt", "md", "docx"];
    private static readonly string[] AllowedProcessingStatuses = ["QUEUED", "PROCESSING", "EXTRACTING", "CHUNKING", "INDEXING", "READY", "FAILED"];

    public ListDocumentsQueryValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        RuleFor(x => x.FileType)
            .Must(value => value is null || AllowedFileTypes.Contains(value.Trim().TrimStart('.'), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Chỉ hỗ trợ lọc PDF, TXT, MD hoặc DOCX.");
        RuleFor(x => x.ProcessingStatus)
            .Must(value => value is null || AllowedProcessingStatuses.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Trạng thái xử lý tài liệu không hợp lệ.");
    }
}
