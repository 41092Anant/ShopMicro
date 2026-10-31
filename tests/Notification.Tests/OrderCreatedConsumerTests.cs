namespace Notification.Tests;

using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Notification.Worker.Consumers;

/// <summary>
/// Unit tests for OrderCreatedConsumer verifying message consumption and processing.
/// </summary>
public class OrderCreatedConsumerTests
{
    [Fact]
    public async Task Consume_WhenValidOrderCreatedEvent_ProcessesSuccessfullyAndLogsDispatch()
    {
        // Arrange
        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var consumer = new OrderCreatedConsumer(logger);

        var orderId = Guid.NewGuid();
        var items = new List<OrderItemDto>
        {
            new(Guid.NewGuid(), "Ergonomic Desk", 1, 450.00m, 450.00m, "Furniture"),
            new(Guid.NewGuid(), "Monitor Arm", 2, 75.00m, 150.00m, "Accessories")
        };

        var orderCreatedEvent = new OrderCreatedEvent(
            orderId,
            "alex@example.com",
            600.00m,
            DateTime.UtcNow,
            items
        );

        var context = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context.Message.Returns(orderCreatedEvent);

        // Act
        await consumer.Consume(context);

        // Assert
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(orderId.ToString()) && o.ToString()!.Contains("alex@example.com")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Consume_WhenOrderHasSingleItem_CompletesWithoutErrors()
    {
        // Arrange
        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var consumer = new OrderCreatedConsumer(logger);

        var orderId = Guid.NewGuid();
        var items = new List<OrderItemDto>
        {
            new(Guid.NewGuid(), "USB-C Cable", 1, 15.00m, 15.00m, "Cables")
        };

        var orderCreatedEvent = new OrderCreatedEvent(
            orderId,
            "buyer@example.com",
            15.00m,
            DateTime.UtcNow,
            items
        );

        var context = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context.Message.Returns(orderCreatedEvent);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => consumer.Consume(context));
        Assert.Null(exception);
    }
}
