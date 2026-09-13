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
        ApiRiskLevel riskLevel, bool requiresConfirmation, DateTimeOffset now,
        string? menuParentCode, int displayOrder, bool isMenuVisible, bool isMobile, string? menuUrl)
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
        ApplyMenuMetadata(menuParentCode, displayOrder, isMenuVisible, isMobile, menuUrl);
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

    public string? MenuParentCode { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsMenuVisible { get; private set; } = true;

    public bool IsMobile { get; private set; }

    public string? MenuUrl { get; private set; }

    private readonly List<RoleApiFunction> _roleApiFunctions = new();

    public IReadOnlyCollection<RoleApiFunction> RoleApiFunctions => _roleApiFunctions;

    public static ApiFunction Create(string code, string module, string name, ApiHttpMethod httpMethod, string routeTemplate,
        ApiActionType actionType, ApiRiskLevel riskLevel, DateTimeOffset now,
        bool requiresConfirmation = false, string? description = null,
        string? menuParentCode = null, int displayOrder = 0, bool isMenuVisible = true,
        bool isMobile = false, string? menuUrl = null)
    {
        return new ApiFunction(
            Guid.NewGuid(), ApiFunctionCode.Create(code), module, name, description,
            httpMethod, RouteTemplate.Create(routeTemplate), actionType, riskLevel,
            requiresConfirmation, now, menuParentCode, displayOrder, isMenuVisible, isMobile, menuUrl);
    }

    public static ApiFunction Create(Guid id, string code, string module, string name, ApiHttpMethod httpMethod, string routeTemplate,
        ApiActionType actionType, ApiRiskLevel riskLevel, DateTimeOffset now,
        bool requiresConfirmation = false, string? description = null,
        string? menuParentCode = null, int displayOrder = 0, bool isMenuVisible = true,
        bool isMobile = false, string? menuUrl = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("ApiFunction id cannot be empty.", nameof(id));
        }

        return new ApiFunction(
            id, ApiFunctionCode.Create(code), module, name, description,
            httpMethod, RouteTemplate.Create(routeTemplate), actionType, riskLevel,
            requiresConfirmation, now, menuParentCode, displayOrder, isMenuVisible, isMobile, menuUrl);
    }

    public void Update(
        string module, string name, ApiHttpMethod httpMethod, string routeTemplate,
        ApiActionType actionType, ApiRiskLevel riskLevel, bool requiresConfirmation,
        string? description,
        string? menuParentCode = null, int displayOrder = 0, bool isMenuVisible = true,
        bool isMobile = false, string? menuUrl = null)
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
        ApplyMenuMetadata(menuParentCode, displayOrder, isMenuVisible, isMobile, menuUrl);
    }

    public void UpdateMenuMetadata(
        string? menuParentCode,
        int displayOrder,
        bool isMenuVisible,
        bool isMobile,
        string? menuUrl)
    {
        ApplyMenuMetadata(menuParentCode, displayOrder, isMenuVisible, isMobile, menuUrl);
    }

    public void Deactivate()
    {
        Status = ApiFunctionStatus.Inactive;
    }

    public void Activate()
    {
        Status = ApiFunctionStatus.Active;
    }

    private void ApplyMenuMetadata(
        string? menuParentCode,
        int displayOrder,
        bool isMenuVisible,
        bool isMobile,
        string? menuUrl)
    {
        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");
        }

        MenuParentCode = menuParentCode is null ? null : DomainGuard.RequiredText(menuParentCode, nameof(menuParentCode), 150);
        MenuUrl = menuUrl is null ? null : DomainGuard.RequiredText(menuUrl, nameof(menuUrl), 300);
        DisplayOrder = displayOrder;
        IsMenuVisible = isMenuVisible;
        IsMobile = isMobile;
    }
}
