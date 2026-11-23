using Infrastructure.Dal;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.API.Infrastructure;

public sealed class DatabaseHealthCheck(DrugsBotDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Нет соединения с базой данных");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Ошибка подключения к базе данных", ex);
        }
    }
}
