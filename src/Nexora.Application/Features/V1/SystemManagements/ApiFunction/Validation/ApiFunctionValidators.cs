using FluentValidation;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Command;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Validation;

public sealed class CreateApiFunctionCommandValidator : AbstractValidator<CreateApiFunctionCommand>
{
    public CreateApiFunctionCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Module).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.HttpMethod)
            .IsInEnum()
            .Must(httpMethod => httpMethod != ApiHttpMethod.Any);
        RuleFor(x => x.RouteTemplate).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ActionType).IsInEnum();
        RuleFor(x => x.RiskLevel).IsInEnum();
    }
}

public sealed class UpdateApiFunctionCommandValidator : AbstractValidator<UpdateApiFunctionCommand>
{
    public UpdateApiFunctionCommandValidator()
    {
        RuleFor(x => x.ApiFunctionId).NotEmpty();
        RuleFor(x => x.Module).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.HttpMethod)
            .IsInEnum()
            .Must(httpMethod => httpMethod != ApiHttpMethod.Any);
        RuleFor(x => x.RouteTemplate).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ActionType).IsInEnum();
        RuleFor(x => x.RiskLevel).IsInEnum();
    }
}

public sealed class DeactivateApiFunctionCommandValidator : AbstractValidator<DeactivateApiFunctionCommand>
{
    public DeactivateApiFunctionCommandValidator() => RuleFor(x => x.ApiFunctionId).NotEmpty();
}

public sealed class ReactivateApiFunctionCommandValidator : AbstractValidator<ReactivateApiFunctionCommand>
{
    public ReactivateApiFunctionCommandValidator() => RuleFor(x => x.ApiFunctionId).NotEmpty();
}
