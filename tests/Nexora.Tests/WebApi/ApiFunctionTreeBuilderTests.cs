using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Tests.WebApi;

public sealed class ApiFunctionTreeBuilderTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_groups_every_api_function_under_its_module_and_orders_children()
    {
        var functions = new[]
        {
            CreateFunction("document.upload", "Document", "Upload", 20),
            CreateFunction("document.read", "Document", "Read", 10),
            CreateFunction("chat.stream", "Chat", "Stream", 5)
        };

        var tree = ApiFunctionTreeBuilder.Build(functions);

        Assert.Equal(new[] { "Chat", "Document" }, tree.Select(node => node.FunctionName));
        Assert.Equal("CHAT.STREAM", Assert.Single(tree[0].ChildFunctions).FunctionCode);
        Assert.Equal(
            new[] { "DOCUMENT.READ", "DOCUMENT.UPLOAD" },
            tree[1].ChildFunctions.Select(node => node.FunctionCode));
        Assert.All(tree.SelectMany(node => node.ChildFunctions), node => Assert.NotNull(node.FunctionId));
    }

    private static ApiFunction CreateFunction(string code, string module, string name, int displayOrder) =>
        ApiFunction.Create(
            code,
            module,
            name,
            ApiHttpMethod.Get,
            $"/api/{module.ToLowerInvariant()}/{code.Split('.').Last()}",
            ApiActionType.Read,
            ApiRiskLevel.Low,
            CreatedAt,
            menuParentCode: module.ToUpperInvariant(),
            displayOrder: displayOrder);
}
