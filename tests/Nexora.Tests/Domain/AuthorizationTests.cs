using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Organizations;

namespace Nexora.Tests.Domain;

public sealed class AuthorizationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidMetadata_ShouldStartAtVersionOne()
    {
        var function = ApiFunction.Create(
            "organization.read",
            "organization",
            "Read organization",
            ApiHttpMethod.Get,
            "/api/organizations/{id}",
            ApiActionType.Read,
            ApiRiskLevel.Low,
            Now);

        Assert.Equal("ORGANIZATION.READ", function.Code.Value);
        Assert.Equal(ApiFunctionStatus.Active, function.Status);
        Assert.Equal(1, function.Version);
    }

    [Fact]
    public void Update_ShouldIncrementVersion()
    {
        var function = ApiFunction.Create(
            "organization.read",
            "organization",
            "Read organization",
            ApiHttpMethod.Get,
            "/api/organizations/{id}",
            ApiActionType.Read,
            ApiRiskLevel.Low,
            Now);

        function.Update(
            "organization",
            "Read organization details",
            ApiHttpMethod.Get,
            "/api/organizations/{id}",
            ApiActionType.Read,
            ApiRiskLevel.Medium,
            requiresConfirmation: false,
            description: "Updated description");

        Assert.Equal(2, function.Version);
        Assert.Equal(ApiRiskLevel.Medium, function.RiskLevel);
    }

    [Fact]
    public void RoleApiFunction_OutsideValidityWindow_ShouldNotBeEffective()
    {
        var permission = RoleApiFunction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PermissionEffect.Allow, Guid.NewGuid(), Now, Now.AddHours(1));

        Assert.False(permission.IsEffectiveAt(Now.AddMinutes(-1)));
        Assert.True(permission.IsEffectiveAt(Now.AddMinutes(30)));
        Assert.False(permission.IsEffectiveAt(Now.AddHours(1)));
    }

    [Fact]
    public void RoleApiFunction_RevokeTwice_ShouldThrowDomainException()
    {
        var permission = RoleApiFunction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PermissionEffect.Deny, Guid.NewGuid(), Now);

        permission.Revoke(Guid.NewGuid(), Now.AddMinutes(1));

        var exception = Assert.Throws<DomainException>(() =>
            permission.Revoke(Guid.NewGuid(), Now.AddMinutes(2)));

        Assert.Equal("role_api_function.already_revoked", exception.Code);
    }

    [Fact]
    public void PermissionPolicy_DenyGrantShouldWinOverAllowGrant()
    {
        var grants = new[]
        {
            RoleApiFunction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PermissionEffect.Allow, Guid.NewGuid(), Now),
            RoleApiFunction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PermissionEffect.Deny, Guid.NewGuid(), Now)
        };

        var decision = ApiPermissionPolicy.Evaluate(grants, Now.AddMinutes(1));

        Assert.Equal(PermissionDecision.Denied, decision);
    }

    [Fact]
    public void OrganizationRoleAssignment_WithInvalidValidityRange_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            OrganizationRoleAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now, Now));

        Assert.Equal("organization_role_assignment.invalid_validity_range", exception.Code);
    }
}
