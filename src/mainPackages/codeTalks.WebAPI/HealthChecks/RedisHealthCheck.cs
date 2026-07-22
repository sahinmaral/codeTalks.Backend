using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace codeTalks.WebAPI.HealthChecks;

public class RedisHealthCheck(IConnectionMultiplexer multiplexer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await multiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Could not connect to Redis.", ex);
        }
    }
}
