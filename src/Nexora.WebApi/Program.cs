using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Nexora.Application.Common.Constants;
using Nexora.Infrastructure;
using Nexora.WebApi;
using Nexora.WebApi.Middleware;
using Nexora.WebApi.Responses;
using Nexora.WebApi.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiErrorDetail(
                JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Giá trị không hợp lệ." : error.ErrorMessage)))
            .ToArray();
        const int statusCode = StatusCodes.Status400BadRequest;
        var response = ApiResponseFactory.Failure(
            context.HttpContext,
            statusCode,
            CommonMessages.Api.RequestValidationFailed,
            CommonMessages.ErrorCodes.Validation,
            details);

        return new BadRequestObjectResult(response);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexora AI API", Version = "v1", Description = "Nexora AI backend API" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT theo dạng: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddNexoraRateLimiting();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async httpContext =>
    {
        const int statusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponseFactory.Failure(
                httpContext,
                statusCode,
                CommonMessages.Api.UnexpectedError,
                CommonMessages.ErrorCodes.Server),
            httpContext.RequestAborted);
    });
});
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var statusCode = httpContext.Response.StatusCode;
    var (message, code) = statusCode switch
    {
        StatusCodes.Status401Unauthorized => (CommonMessages.Api.AuthenticationRequired, CommonMessages.ErrorCodes.Unauthorized),
        StatusCodes.Status403Forbidden => (CommonMessages.Api.Forbidden, CommonMessages.ErrorCodes.Forbidden),
        StatusCodes.Status404NotFound => (CommonMessages.Api.EndpointNotFound, CommonMessages.ErrorCodes.NotFound),
        StatusCodes.Status405MethodNotAllowed => (CommonMessages.Api.MethodNotAllowed, "http.method_not_allowed"),
        _ => (CommonMessages.Api.DefaultError, CommonMessages.ErrorCodes.Server)
    };

    await httpContext.Response.WriteAsJsonAsync(
        ApiResponseFactory.Failure(httpContext, statusCode, message, code),
        httpContext.RequestAborted);
});
app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexora AI API v1"); options.RoutePrefix = "swagger"; });
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseMiddleware<ApiPermissionMiddleware>();

app.MapControllers();

app.Run();
