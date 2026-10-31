using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Order.Api.Health;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IBusControl _busControl;

    public RabbitMqHealthCheck(IBusControl busControl)
    {
        _busControl = busControl;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _busControl.CheckHealth();
            return result.Status switch
            {
                BusHealthStatus.Healthy => Task.FromResult(HealthCheckResult.Healthy(result.Description ?? "RabbitMQ bus is healthy.")),
                BusHealthStatus.Degraded => Task.FromResult(HealthCheckResult.Degraded(result.Description ?? "RabbitMQ bus is degraded.", result.Exception)),
                _ => Task.FromResult(HealthCheckResult.Unhealthy(result.Description ?? "RabbitMQ bus is unhealthy.", result.Exception))
            };
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ bus health check failed.", ex));
        }
    }
}
