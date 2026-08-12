using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Application.Common.Constants;
using Nexora.WebApi;
using Nexora.WebApi.Responses;

namespace Nexora.Tests.WebApi;

public sealed class ApiResponseTests
{
    [Fact]
    public void Success_ShouldContainDataAndMetaWithoutError()
    {
        var context = CreateContext();

        var response = ApiResponseFactory.Success(context, StatusCodes.Status200OK, "Thành công.", new { Id = 123 });
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(StatusCodes.Status200OK, root.GetProperty("statusCode").GetInt32());
        Assert.Equal(123, root.GetProperty("data").GetProperty("id").GetInt32());
        Assert.False(root.TryGetProperty("error", out _));
        Assert.Equal("/api/users", root.GetProperty("meta").GetProperty("path").GetString());
        Assert.Equal("request-123", root.GetProperty("meta").GetProperty("requestId").GetString());
    }

    [Fact]
    public async Task GlobalExceptionHandler_ValidationError_ShouldReturnConsistentEnvelope()
    {
        var context = CreateContext();
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var exception = new ValidationException([new ValidationFailure("Email", "Email không hợp lệ.")]);

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(StatusCodes.Status400BadRequest, root.GetProperty("statusCode").GetInt32());
        Assert.Equal(CommonMessages.ErrorCodes.Validation, root.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("email", root.GetProperty("error").GetProperty("details")[0].GetProperty("field").GetString());
        Assert.False(root.TryGetProperty("data", out _));
        Assert.Equal("request-123", root.GetProperty("meta").GetProperty("requestId").GetString());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "request-123" };
        context.Request.Path = "/api/users";
        return context;
    }
}
