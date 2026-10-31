using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Catalog.Api.Health;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _connection;

    public RedisHealthCheck(IConnectionMultiplexer? connection = null)
    {
        _connection = connection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_connection == null || !_connection.IsConnected)
        {
            return HealthCheckResult.Unhealthy("Redis is not connected.");
        }

        try
        {
            var latency = await _connection.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis responded in {latency.TotalMilliseconds:F1}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis ping failed.", ex);
        }
    }
}
