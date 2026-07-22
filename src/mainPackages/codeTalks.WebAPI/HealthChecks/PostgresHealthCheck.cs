using codeTalks.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace codeTalks.WebAPI.HealthChecks;

public class PostgresHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Could not connect to Postgres.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Could not connect to Postgres.", ex);
        }
    }
}
