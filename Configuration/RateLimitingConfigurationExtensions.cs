using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace FileWorkspace.Configuration;

public static class RateLimitingConfigurationExtensions
{
    public static IServiceCollection AddFileWorkspaceRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Click-driven endpoints (list/delete/folder/download-ticket): a human rarely calls
            // these more than a few dozen times a minute, so this stays tight against token/ticket guessing.
            // No queue: once the ceiling is hit, fail fast with 429 instead of holding requests open.
            options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            // Upload session lifecycle (start/resume/cancel/chunk): "Upload folder" can fire hundreds
            // of session-start calls at once, and chunk PUTs stream continuously on fast networks.
            // Sized to absorb that legitimate burst while still capping a runaway/scripted client.
            options.AddPolicy("api-uploads", context => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 2000,
                    TokensPerPeriod = 500,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    QueueLimit = 2000,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));
        });
    }
}
