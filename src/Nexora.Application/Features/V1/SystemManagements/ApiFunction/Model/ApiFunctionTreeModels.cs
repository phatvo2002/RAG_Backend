using DomainApiFunction = Nexora.Domain.Authorization.ApiFunction;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;

public sealed record ApiFunctionTreeResponse
{
    public Guid? FunctionId { get; init; }
    public string FunctionCode { get; init; } = string.Empty;
    public string FunctionName { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Url { get; init; }
    public string? ApiEndpoint { get; init; }
    public string? ParentCode { get; init; }
    public int Order { get; init; }
    public string? Status { get; init; }
    public string? HttpMethod { get; init; }
    public string? RouteTemplate { get; init; }
    public string? ActionType { get; init; }
    public string? RiskLevel { get; init; }
    public bool RequiresConfirmation { get; init; }
    public int Version { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyList<ApiFunctionTreeResponse> ChildFunctions { get; init; } = Array.Empty<ApiFunctionTreeResponse>();
}

public static class ApiFunctionTreeBuilder
{
    public static IReadOnlyList<ApiFunctionTreeResponse> Build(IReadOnlyCollection<DomainApiFunction> functions) =>
        functions
            .GroupBy(function => function.Module, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ApiFunctionTreeResponse
            {
                FunctionCode = group.Key.ToUpperInvariant(),
                FunctionName = group.Key,
                Module = group.Key,
                Order = group.Min(function => function.DisplayOrder),
                ChildFunctions = group
                    .OrderBy(function => function.DisplayOrder)
                    .ThenBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(ToResponse)
                    .ToArray()
            })
            .ToArray();

    private static ApiFunctionTreeResponse ToResponse(DomainApiFunction function) =>
        new()
        {
            FunctionId = function.Id,
            FunctionCode = function.Code.Value,
            FunctionName = function.Name,
            Module = function.Module,
            Description = function.Description,
            Url = function.MenuUrl,
            ApiEndpoint = function.RouteTemplate.Value == "#"
                ? "#"
                : $"{function.HttpMethod} {function.RouteTemplate.Value}",
            ParentCode = function.MenuParentCode,
            Order = function.DisplayOrder,
            Status = function.Status.ToString(),
            HttpMethod = function.HttpMethod.ToString(),
            RouteTemplate = function.RouteTemplate.Value,
            ActionType = function.ActionType.ToString(),
            RiskLevel = function.RiskLevel.ToString(),
            RequiresConfirmation = function.RequiresConfirmation,
            Version = function.Version,
            CreatedAt = function.CreatedAt,
            UpdatedAt = function.UpdatedAt
        };
}
