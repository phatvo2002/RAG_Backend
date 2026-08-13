using Nexora.Domain.Authorization.Enums;
using DomainApiFunction = Nexora.Domain.Authorization.ApiFunction;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;

public class ApiFunctionResponse
{
    public ApiFunctionResponse()
    {
    }

    public ApiFunctionResponse(
        Guid apiFunctionId,
        string code,
        string module,
        string name,
        string? description,
        string httpMethod,
        string routeTemplate,
        string actionType,
        string riskLevel,
        bool requiresConfirmation,
        string status,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        ApiFunctionId = apiFunctionId;
        Code = code;
        Module = module;
        Name = name;
        Description = description;
        HttpMethod = httpMethod;
        RouteTemplate = routeTemplate;
        ActionType = actionType;
        RiskLevel = riskLevel;
        RequiresConfirmation = requiresConfirmation;
        Status = status;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid ApiFunctionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string RouteTemplate { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public bool RequiresConfirmation { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static ApiFunctionResponse FromEntity(DomainApiFunction function) =>
        new(
            function.Id,
            function.Code.Value,
            function.Module,
            function.Name,
            function.Description,
            function.HttpMethod.ToString(),
            function.RouteTemplate.Value,
            function.ActionType.ToString(),
            function.RiskLevel.ToString(),
            function.RequiresConfirmation,
            function.Status.ToString(),
            function.Version,
            function.CreatedAt,
            function.UpdatedAt);
}

public class CreateApiFunctionRequest
{
    public CreateApiFunctionRequest()
    {
    }

    public CreateApiFunctionRequest(
        string code,
        string module,
        string name,
        ApiHttpMethod httpMethod,
        string routeTemplate,
        ApiActionType actionType,
        ApiRiskLevel riskLevel,
        bool requiresConfirmation = false,
        string? description = null)
    {
        Code = code;
        Module = module;
        Name = name;
        HttpMethod = httpMethod;
        RouteTemplate = routeTemplate;
        ActionType = actionType;
        RiskLevel = riskLevel;
        RequiresConfirmation = requiresConfirmation;
        Description = description;
    }

    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ApiHttpMethod HttpMethod { get; set; }
    public string RouteTemplate { get; set; } = string.Empty;
    public ApiActionType ActionType { get; set; }
    public ApiRiskLevel RiskLevel { get; set; }
    public bool RequiresConfirmation { get; set; }
}

public class UpdateApiFunctionRequest
{
    public UpdateApiFunctionRequest()
    {
    }

    public UpdateApiFunctionRequest(
        string module,
        string name,
        ApiHttpMethod httpMethod,
        string routeTemplate,
        ApiActionType actionType,
        ApiRiskLevel riskLevel,
        bool requiresConfirmation = false,
        string? description = null)
    {
        Module = module;
        Name = name;
        HttpMethod = httpMethod;
        RouteTemplate = routeTemplate;
        ActionType = actionType;
        RiskLevel = riskLevel;
        RequiresConfirmation = requiresConfirmation;
        Description = description;
    }

    public string Module { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ApiHttpMethod HttpMethod { get; set; }
    public string RouteTemplate { get; set; } = string.Empty;
    public ApiActionType ActionType { get; set; }
    public ApiRiskLevel RiskLevel { get; set; }
    public bool RequiresConfirmation { get; set; }
}
