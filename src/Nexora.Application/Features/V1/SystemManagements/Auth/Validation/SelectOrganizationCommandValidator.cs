using FluentValidation;
using Nexora.Application.Features.V1.SystemManagements.Auth.Command;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Validation;

public sealed class SelectOrganizationCommandValidator : AbstractValidator<SelectOrganizationCommand>
{
    public SelectOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();
    }
}
