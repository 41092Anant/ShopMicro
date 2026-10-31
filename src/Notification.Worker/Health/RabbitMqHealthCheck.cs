using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notification.Worker.Health;

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
                BusHealthStatus.Healthy => Task.FromResult(HealthCheckResult.Healthy(result.Description ?? "RabbitMQ consumer bus is healthy.")),
                BusHealthStatus.Degraded => Task.FromResult(HealthCheckResult.Degraded(result.Description ?? "RabbitMQ consumer bus is degraded.", result.Exception)),
                _ => Task.FromResult(HealthCheckResult.Unhealthy(result.Description ?? "RabbitMQ consumer bus is unhealthy.", result.Exception))
            };
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ consumer bus health check failed.", ex));
        }
    }
}
