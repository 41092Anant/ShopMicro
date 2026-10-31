using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Order.Api.Health;

namespace Order.Tests;

public class RabbitMqHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenBusHealthy_ReturnsHealthy()
    {
        // Arrange
        var busControl = Substitute.For<IBusControl>();
        var busHealth = BusHealthResult.Healthy("All endpoints active", new Dictionary<string, EndpointHealthResult>());
        busControl.CheckHealth().Returns(busHealth);

        var healthCheck = new RabbitMqHealthCheck(busControl);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("All endpoints active", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBusDegraded_ReturnsDegraded()
    {
        // Arrange
        var busControl = Substitute.For<IBusControl>();
        var busHealth = BusHealthResult.Degraded("One endpoint starting", null!, new Dictionary<string, EndpointHealthResult>());
        busControl.CheckHealth().Returns(busHealth);

        var healthCheck = new RabbitMqHealthCheck(busControl);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("One endpoint starting", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBusUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var busControl = Substitute.For<IBusControl>();
        var busHealth = BusHealthResult.Unhealthy("Broker disconnected", new InvalidOperationException("Conn refused"), new Dictionary<string, EndpointHealthResult>());
        busControl.CheckHealth().Returns(busHealth);

        var healthCheck = new RabbitMqHealthCheck(busControl);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Broker disconnected", result.Description);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCheckHealthThrows_ReturnsUnhealthy()
    {
        // Arrange
        var busControl = Substitute.For<IBusControl>();
        busControl.CheckHealth().Returns(_ => throw new Exception("Fatal bus error"));

        var healthCheck = new RabbitMqHealthCheck(busControl);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("RabbitMQ bus health check failed.", result.Description);
        Assert.NotNull(result.Exception);
    }
}
