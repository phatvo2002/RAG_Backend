using FluentValidation;
using Nexora.Application.Features.V1.SystemManagements.Role.Command;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Validation;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
}

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
    }
}

public sealed class GrantRolePermissionCommandValidator : AbstractValidator<GrantRolePermissionCommand>
{
    public GrantRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.FunctionCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExpiresAt).GreaterThan(x => x.ValidFrom).When(x => x.ExpiresAt.HasValue && x.ValidFrom.HasValue);
    }
}

public sealed class SyncRolePermissionsCommandValidator : AbstractValidator<SyncRolePermissionsCommand>
{
    public SyncRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.FunctionIds).NotNull().Must(functionIds => functionIds is not null && functionIds.Count <= 1000);
        RuleForEach(x => x.FunctionIds).NotEmpty();
    }
}
