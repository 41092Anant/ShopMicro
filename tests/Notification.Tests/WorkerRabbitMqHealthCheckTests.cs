using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notification.Worker.Health;
using NSubstitute;

namespace Notification.Tests;

public class WorkerRabbitMqHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenBusHealthy_ReturnsHealthy()
    {
        // Arrange
        var busControl = Substitute.For<IBusControl>();
        var busHealth = BusHealthResult.Healthy("Consumer queue online", new Dictionary<string, EndpointHealthResult>());
        busControl.CheckHealth().Returns(busHealth);

        var healthCheck = new RabbitMqHealthCheck(busControl);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Consumer queue online", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBusUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var busControl = Substitute.For<IBusControl>();
        var busHealth = BusHealthResult.Unhealthy("RabbitMQ connection lost", new Exception("Broken pipe"), new Dictionary<string, EndpointHealthResult>());
        busControl.CheckHealth().Returns(busHealth);

        var healthCheck = new RabbitMqHealthCheck(busControl);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("RabbitMQ connection lost", result.Description);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBusThrows_ReturnsUnhealthy()
    {
        // Arrange
        var busControl = Substitute.For<IBusControl>();
        busControl.CheckHealth().Returns(_ => throw new InvalidOperationException("Bus fault"));

        var healthCheck = new RabbitMqHealthCheck(busControl);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("RabbitMQ consumer bus health check failed.", result.Description);
        Assert.NotNull(result.Exception);
    }
}
