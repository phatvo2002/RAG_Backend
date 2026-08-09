using FluentValidation;
using Nexora.Application.Features.V1.SystemManagements.Organization.Command;

namespace Nexora.Application.Features.V1.SystemManagements.Organization.Validation;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataResidency).MaximumLength(50);
    }
}

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataResidency).MaximumLength(50);
    }
}
