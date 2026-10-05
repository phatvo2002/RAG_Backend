using FluentValidation;

using Nexora.Application.Features.V1.Rag.Retrieval.Query;

namespace Nexora.Application.Features.V1.Rag.Retrieval.Validation;

public sealed class RetrieveDocumentsQueryValidator : AbstractValidator<RetrieveDocumentsQuery>
{
    public RetrieveDocumentsQueryValidator()
    {
        RuleFor(request => request.Query)
            .NotEmpty()
            .Must(query => !string.IsNullOrWhiteSpace(query))
            .WithMessage("Question must not be empty.")
            .MaximumLength(2000);
    }
}
