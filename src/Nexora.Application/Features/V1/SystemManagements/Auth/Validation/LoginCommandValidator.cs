using FluentValidation;
using Nexora.Application.Features.V1.SystemManagements.Auth.Command;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Validation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);

    }
}
