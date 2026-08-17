using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Application.Common.Constants;
using Nexora.WebApi.Responses;

namespace Nexora.WebApi.RateLimiting;

internal static class RateLimitConfiguration
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddNexoraRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => CreateLimiter(RateLimitPartitionKeys.Global(context), 120));
            options.AddPolicy("auth", context => CreateLimiter(RateLimitPartitionKeys.Auth(context), 10));
            options.AddPolicy("write", context => CreateLimiter(RateLimitPartitionKeys.Write(context), 30));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var retryAfterSeconds = (int)Window.TotalSeconds;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                }

                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponseFactory.Failure(
                        context.HttpContext,
                        StatusCodes.Status429TooManyRequests,
                        CommonMessages.Api.TooManyRequests,
                        CommonMessages.ErrorCodes.RateLimitExceeded),
                    cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> CreateLimiter(string partitionKey, int permitLimit) =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = Window,
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
}
