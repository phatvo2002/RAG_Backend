using FluentValidation;
using Nexora.Application.Features.V1.Documents.Command;

namespace Nexora.Application.Features.V1.Documents.Validation;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly string[] AllowedExtensions = [".pdf", ".txt", ".md", ".docx"];

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500).Must(HasAllowedExtension).WithMessage("Chỉ hỗ trợ file PDF, TXT, MD hoặc DOCX.");
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(25 * 1024 * 1024);
        RuleFor(x => x.Content).NotNull();
    }

    private static bool HasAllowedExtension(string fileName) => AllowedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
}
