using Catalog.Api.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;

namespace Catalog.Tests;

public class RedisHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenConnectionIsNull_ReturnsUnhealthy()
    {
        // Arrange
        var healthCheck = new RedisHealthCheck(null);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis is not connected.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionIsNotConnected_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        connection.IsConnected.Returns(false);
        var healthCheck = new RedisHealthCheck(connection);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis is not connected.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectedAndPingSucceeds_ReturnsHealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        connection.IsConnected.Returns(true);
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        database.PingAsync(Arg.Any<CommandFlags>()).Returns(Task.FromResult(TimeSpan.FromMilliseconds(5.2)));

        var healthCheck = new RedisHealthCheck(connection);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Redis responded in 5.2ms", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingThrows_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        connection.IsConnected.Returns(true);
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        database.PingAsync(Arg.Any<CommandFlags>()).ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "Timeout"));

        var healthCheck = new RedisHealthCheck(connection);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis ping failed.", result.Description);
        Assert.NotNull(result.Exception);
    }
}
