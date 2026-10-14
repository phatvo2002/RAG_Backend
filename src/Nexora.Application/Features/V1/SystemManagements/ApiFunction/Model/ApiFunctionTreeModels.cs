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
            .Select(group =>
            {
                var moduleFunctions = group.ToArray();
                var menuParents = moduleFunctions
                    .Where(function => function.RouteTemplate.Value == "#")
                    .ToDictionary(function => function.Code.Value, StringComparer.OrdinalIgnoreCase);
                var moduleParent = menuParents.Values.FirstOrDefault(function =>
                    NormalizeCode(function.Code.Value) == NormalizeCode(group.Key));

                IReadOnlyList<ApiFunctionTreeResponse> BuildChildren(string parentCode) => moduleFunctions
                    .Where(function => string.Equals(function.MenuParentCode, parentCode, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(function => function.DisplayOrder)
                    .ThenBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(function => ToTreeNode(function, BuildChildren))
                    .Where(node => node is not null)
                    .Select(node => node!)
                    .ToArray();

                var rootFunctions = new List<ApiFunctionTreeResponse>();
                if (moduleParent is not null)
                {
                    rootFunctions.AddRange(BuildChildren(moduleParent.Code.Value));
                }
                else
                {
                    rootFunctions.AddRange(BuildChildren(group.Key.ToUpperInvariant()));
                }

                rootFunctions.AddRange(moduleFunctions
                    .Where(function => string.IsNullOrWhiteSpace(function.MenuParentCode) &&
                        function.RouteTemplate.Value != "#" &&
                        !ReferenceEquals(function, moduleParent))
                    .OrderBy(function => function.DisplayOrder)
                    .ThenBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(function => ToResponse(function) with
                    {
                        ChildFunctions = BuildChildren(function.Code.Value)
                    }));

                rootFunctions.AddRange(menuParents.Values
                    .Where(function => function.MenuParentCode is null && !ReferenceEquals(function, moduleParent))
                    .OrderBy(function => function.DisplayOrder)
                    .ThenBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(function => ToStructuralNode(function, BuildChildren(function.Code.Value)))
                    .Where(node => node.ChildFunctions.Count > 0));

                var rootName = moduleParent?.Name ?? group.Key;

                return new ApiFunctionTreeResponse
                {
                    FunctionCode = moduleParent?.Code.Value ?? group.Key.ToUpperInvariant(),
                    FunctionName = rootName,
                    Module = group.Key,
                    Order = moduleFunctions.Min(function => function.DisplayOrder),
                    ChildFunctions = rootFunctions
                        .OrderBy(node => node.Order)
                        .ThenBy(node => node.FunctionName, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                };
            })
            .ToArray();

    private static string NormalizeCode(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static ApiFunctionTreeResponse? ToTreeNode(
        DomainApiFunction function,
        Func<string, IReadOnlyList<ApiFunctionTreeResponse>> buildChildren)
    {
        var children = buildChildren(function.Code.Value);
        return function.RouteTemplate.Value == "#"
            ? children.Count == 0 ? null : ToStructuralNode(function, children)
            : ToResponse(function) with { ChildFunctions = children };
    }

    private static ApiFunctionTreeResponse ToStructuralNode(
        DomainApiFunction function,
        IReadOnlyList<ApiFunctionTreeResponse> children) =>
        new()
        {
            FunctionId = null,
            FunctionCode = function.Code.Value,
            FunctionName = function.Name,
            Module = function.Module,
            Description = function.Description,
            ParentCode = function.MenuParentCode,
            Order = function.DisplayOrder,
            ChildFunctions = children
        };

    internal static ApiFunctionTreeResponse ToResponse(DomainApiFunction function) =>
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
