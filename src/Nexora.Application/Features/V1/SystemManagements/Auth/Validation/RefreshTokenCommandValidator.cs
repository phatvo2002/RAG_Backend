using FluentValidation;
using Nexora.Application.Features.V1.SystemManagements.Auth.Command;

namespace Nexora.Application.Features.V1.SystemManagements.Auth.Validation;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(512);
    }
}
