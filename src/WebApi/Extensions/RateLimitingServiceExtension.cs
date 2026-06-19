using System;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.Core.SecurityModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fistix.TaskManager.WebApi.Extensions;

public static class RateLimitingServiceExtension
{
    public static IServiceCollection AddAiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var aiConfig = new AiConfiguration();
        configuration.GetSection("Ai").Bind(aiConfig);
        var rateLimit = aiConfig.Features.SummarizeRateLimit;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "Too many requests",
                    Detail = "AI summarization rate limit exceeded. Please try again later.",
                    Status = StatusCodes.Status429TooManyRequests
                }, cancellationToken);
            };

            if (!rateLimit.Enabled)
            {
                options.AddPolicy(RateLimitPolicies.AiSummarize, _ =>
                    RateLimitPartition.GetNoLimiter(RateLimitPolicies.AiSummarize));
                return;
            }

            var permitLimit = Math.Max(1, rateLimit.PermitLimit);
            var window = TimeSpan.FromMinutes(Math.Max(1, rateLimit.WindowMinutes));

            options.AddPolicy(RateLimitPolicies.AiSummarize, httpContext =>
            {
                var partitionKey = httpContext.User?.FindFirstValue("sub")
                    ?? httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        return services;
    }
}
