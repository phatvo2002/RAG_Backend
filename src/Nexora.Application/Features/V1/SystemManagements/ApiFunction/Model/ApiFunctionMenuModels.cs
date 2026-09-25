using DomainApiFunction = Nexora.Domain.Authorization.ApiFunction;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;

public sealed record ApiFunctionMenuResponse
{
    public Guid? FunctionId { get; init; }
    public string FunctionCode { get; init; } = string.Empty;
    public string FunctionName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Url { get; init; }
    public string? ApiEndpoint { get; init; }
    public string? ParentCode { get; init; }
    public int Order { get; init; }
    public bool IsMobile { get; init; }
    public IReadOnlyList<ApiFunctionMenuResponse> ChildFunctions { get; init; } = Array.Empty<ApiFunctionMenuResponse>();
}

public static class ApiFunctionMenuTreeBuilder
{
    private static readonly IReadOnlyList<MenuGroupDefinition> Groups =
    [
        new("auth", "Auth", "Auth controller", null, 1),
        new("user", "User", "User controller", null, 2),
        new("activity_log", "Activity Log", "Activity log controller", null, 3),
        new("role", "Role", "Role controller", null, 4),
        new("document", "Document", "Document controller", null, 5),
        new("authorization", "Authorization", "ApiFunction controller", null, 6),
        new("department", "Department", "Department controller", null, 7),
        new("organization", "Organization", "Organization controller", null, 8)
    ];

    public static IReadOnlyList<ApiFunctionMenuResponse> Build(IReadOnlyCollection<DomainApiFunction> functions)
    {
        var nodes = Groups.ToDictionary(
            group => group.Code,
            group => new MutableMenuNode(
                group.Code,
                group.Name,
                group.Description,
                group.ParentCode,
                group.DisplayOrder,
                group.IsMobile),
            StringComparer.OrdinalIgnoreCase);

        foreach (var function in functions.Where(item => item.IsMenuVisible))
        {
            var code = function.Code.Value;
            nodes[code] = new MutableMenuNode(
                code,
                function.Name,
                function.Description,
                function.MenuParentCode,
                function.DisplayOrder,
                function.IsMobile,
                function.Id,
                function.MenuUrl,
                function.RouteTemplate.Value == "#"
                    ? "#"
                    : $"{function.HttpMethod} {function.RouteTemplate.Value}");
        }

        var roots = new List<MutableMenuNode>();
        foreach (var node in nodes.Values)
        {
            if (node.ParentCode is null)
            {
                roots.Add(node);
                continue;
            }

            if (nodes.TryGetValue(node.ParentCode, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots
            .Where(node => node.HasVisibleDescendant())
            .OrderBy(node => node.DisplayOrder)
            .ThenBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToResponse)
            .ToArray();
    }

    private static ApiFunctionMenuResponse ToResponse(MutableMenuNode node) =>
        new()
        {
            FunctionId = node.FunctionId,
            FunctionCode = node.Code,
            FunctionName = node.Name,
            Description = node.Description,
            Url = node.Url,
            ApiEndpoint = node.ApiEndpoint,
            ParentCode = node.ParentCode,
            Order = node.DisplayOrder,
            IsMobile = node.IsMobile,
            ChildFunctions = node.Children
                .Where(child => child.HasVisibleDescendant())
                .OrderBy(child => child.DisplayOrder)
                .ThenBy(child => child.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToResponse)
                .ToArray()
        };

    private sealed class MutableMenuNode(
        string code,
        string name,
        string? description,
        string? parentCode,
        int displayOrder,
        bool isMobile,
        Guid? functionId = null,
        string? url = null,
        string? apiEndpoint = null)
    {
        public string Code { get; } = code;
        public string Name { get; } = name;
        public string? Description { get; } = description;
        public string? ParentCode { get; } = parentCode;
        public int DisplayOrder { get; } = displayOrder;
        public bool IsMobile { get; } = isMobile;
        public Guid? FunctionId { get; } = functionId;
        public string? Url { get; } = url;
        public string? ApiEndpoint { get; } = apiEndpoint;
        public List<MutableMenuNode> Children { get; } = [];

        public bool HasVisibleDescendant() =>
            FunctionId.HasValue || Children.Any(child => child.HasVisibleDescendant());
    }

    private sealed record MenuGroupDefinition(
        string Code,
        string Name,
        string Description,
        string? ParentCode,
        int DisplayOrder,
        bool IsMobile = false);
}
