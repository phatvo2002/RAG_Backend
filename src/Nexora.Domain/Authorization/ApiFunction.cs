using Nexora.Domain.Common;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Domain.Authorization;

public sealed class ApiFunction : AuditableEntityBase
{
    private ApiFunction()
    {
    }

    private ApiFunction(
        Guid id, ApiFunctionCode code, string module, string name, string? description,
        ApiHttpMethod httpMethod, RouteTemplate routeTemplate, ApiActionType actionType,
        ApiRiskLevel riskLevel, bool requiresConfirmation, DateTimeOffset now)
        : base(id, now)
    {
        Code = code;
        Module = DomainGuard.RequiredText(module, nameof(module), 80);
        Name = DomainGuard.RequiredText(name, nameof(name), 200);
        Description = description is null
            ? null
            : DomainGuard.RequiredText(description, nameof(description), 500);
        HttpMethod = httpMethod;
        RouteTemplate = routeTemplate;
        ActionType = actionType;
        RiskLevel = riskLevel;
        RequiresConfirmation = requiresConfirmation;
        Status = ApiFunctionStatus.Active;
        Version = 1;
    }

    public ApiFunctionCode Code { get; private set; } = null!;

    public string Module { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public ApiHttpMethod HttpMethod { get; private set; }

    public RouteTemplate RouteTemplate { get; private set; } = null!;

    public ApiActionType ActionType { get; private set; }

    public ApiRiskLevel RiskLevel { get; private set; }

    public bool RequiresConfirmation { get; private set; }

    public ApiFunctionStatus Status { get; private set; }

    public int Version { get; private set; }

    private readonly List<RoleApiFunction> _roleApiFunctions = new();

    public IReadOnlyCollection<RoleApiFunction> RoleApiFunctions => _roleApiFunctions;

    public static ApiFunction Create(string code, string module, string name, ApiHttpMethod httpMethod, string routeTemplate,
        ApiActionType actionType, ApiRiskLevel riskLevel, DateTimeOffset now,
        bool requiresConfirmation = false, string? description = null)
    {
        return new ApiFunction(
            Guid.NewGuid(), ApiFunctionCode.Create(code), module, name, description,
            httpMethod, RouteTemplate.Create(routeTemplate), actionType, riskLevel,
            requiresConfirmation, now);
    }

    public void Update(
        string module, string name, ApiHttpMethod httpMethod, string routeTemplate,
        ApiActionType actionType, ApiRiskLevel riskLevel, bool requiresConfirmation,
        string? description)
    {
        Module = DomainGuard.RequiredText(module, nameof(module), 80);
        Name = DomainGuard.RequiredText(name, nameof(name), 200);
        Description = description is null
            ? null
            : DomainGuard.RequiredText(description, nameof(description), 500);
        HttpMethod = httpMethod;
        RouteTemplate = RouteTemplate.Create(routeTemplate);
        ActionType = actionType;
        RiskLevel = riskLevel;
        RequiresConfirmation = requiresConfirmation;
        Version++;
    }

    public void Deactivate()
    {
        Status = ApiFunctionStatus.Inactive;
    }

    public void Activate()
    {
        Status = ApiFunctionStatus.Active;
    }
}
