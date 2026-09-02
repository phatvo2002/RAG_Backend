using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Application.Common.Abstractions.Logging;
using Nexora.WebApi.Middleware;

namespace Nexora.Tests.WebApi;

public sealed class RequestActivityLogMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldWriteActivityLogForSuccessfulWriteRequest()
    {
        var writer = new TestActivityLogWriter();
        var middleware = new RequestActivityLogMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }, NullLogger<RequestActivityLogMiddleware>.Instance);
        var context = CreateContext(HttpMethods.Post);

        await middleware.InvokeAsync(context, writer);

        Assert.NotNull(writer.Entry);
        Assert.Equal(HttpMethods.Post, writer.Entry!.Action);
        Assert.Equal("SUCCESS", writer.Entry.Result);
        Assert.Equal(StatusCodes.Status204NoContent, writer.Entry.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldWriteFailedActivityLogWhenRequestThrows()
    {
        var writer = new TestActivityLogWriter();
        var middleware = new RequestActivityLogMiddleware(_ => throw new InvalidOperationException("test"), NullLogger<RequestActivityLogMiddleware>.Instance);
        var context = CreateContext(HttpMethods.Post);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context, writer));

        Assert.NotNull(writer.Entry);
        Assert.Equal("FAILURE", writer.Entry!.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, writer.Entry.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotWriteActivityLogForReadRequest()
    {
        var writer = new TestActivityLogWriter();
        var executed = false;
        var middleware = new RequestActivityLogMiddleware(context =>
        {
            executed = true;
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        }, NullLogger<RequestActivityLogMiddleware>.Instance);
        var context = CreateContext(HttpMethods.Get);

        await middleware.InvokeAsync(context, writer);

        Assert.True(executed);
        Assert.Null(writer.Entry);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task InvokeAsync_ShouldWriteActivityLogForEachWriteMethod(string method)
    {
        var writer = new TestActivityLogWriter();
        var middleware = new RequestActivityLogMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        }, NullLogger<RequestActivityLogMiddleware>.Instance);
        var context = CreateContext(method);

        await middleware.InvokeAsync(context, writer);

        Assert.NotNull(writer.Entry);
        Assert.Equal(method, writer.Entry!.Action);
    }

    private static DefaultHttpContext CreateContext(string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = "/api/test";
        context.TraceIdentifier = "test-request";
        return context;
    }

    private sealed class TestActivityLogWriter : IActivityLogWriter
    {
        public ActivityLogEntry? Entry { get; private set; }

        public Task WriteAsync(ActivityLogEntry entry, CancellationToken cancellationToken)
        {
            Entry = entry;
            return Task.CompletedTask;
        }
    }
}
