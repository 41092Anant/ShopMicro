namespace Notification.Tests;

using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Notification.Worker.Consumers;
using Notification.Worker.Services;

/// <summary>
/// Unit tests for OrderCreatedConsumer and idempotency handling.
/// </summary>
public class OrderCreatedConsumerTests
{
    [Fact]
    public async Task Consume_WhenValidOrderCreatedEvent_ProcessesSuccessfullyAndLogsDispatch()
    {
        // Arrange
        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var idempotencyStore = new InMemoryProcessedMessageStore();
        var consumer = new OrderCreatedConsumer(idempotencyStore, logger);

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
        context.CancellationToken.Returns(CancellationToken.None);

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
    public async Task Consume_WhenDuplicateOrderReceived_SkipsProcessingAndLogsWarning()
    {
        // Arrange
        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var idempotencyStore = new InMemoryProcessedMessageStore();
        var consumer = new OrderCreatedConsumer(idempotencyStore, logger);

        var orderId = Guid.NewGuid();
        var items = new List<OrderItemDto>
        {
            new(Guid.NewGuid(), "Mechanical Keyboard", 1, 120.00m, 120.00m, "Peripherals")
        };

        var orderCreatedEvent = new OrderCreatedEvent(
            orderId,
            "duplicate@example.com",
            120.00m,
            DateTime.UtcNow,
            items
        );

        var context = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context.Message.Returns(orderCreatedEvent);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act - First delivery (should process)
        await consumer.Consume(context);

        // Act - Duplicate delivery (should be skipped)
        await consumer.Consume(context);

        // Assert - Warning logged for duplicate message
        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Duplicate OrderCreatedEvent detected") && o.ToString()!.Contains(orderId.ToString())),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        // Assert - Dispatch log was called exactly ONCE across both attempts
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Dispatching order confirmation notification")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Consume_WhenDistinctOrdersReceived_ProcessesBothIndependently()
    {
        // Arrange
        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var idempotencyStore = new InMemoryProcessedMessageStore();
        var consumer = new OrderCreatedConsumer(idempotencyStore, logger);

        var order1 = new OrderCreatedEvent(
            Guid.NewGuid(),
            "first@example.com",
            50.00m,
            DateTime.UtcNow,
            new List<OrderItemDto> { new(Guid.NewGuid(), "Mousepad", 1, 50.00m, 50.00m, "Accessories") }
        );

        var order2 = new OrderCreatedEvent(
            Guid.NewGuid(),
            "second@example.com",
            150.00m,
            DateTime.UtcNow,
            new List<OrderItemDto> { new(Guid.NewGuid(), "Headset", 1, 150.00m, 150.00m, "Audio") }
        );

        var context1 = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context1.Message.Returns(order1);
        context1.CancellationToken.Returns(CancellationToken.None);

        var context2 = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context2.Message.Returns(order2);
        context2.CancellationToken.Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context1);
        await consumer.Consume(context2);

        // Assert - Both orders were dispatched
        logger.Received(2).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Dispatching order confirmation notification")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InMemoryProcessedMessageStore_WhenKeyAddedTwice_ReturnsTrueThenFalse()
    {
        // Arrange
        var store = new InMemoryProcessedMessageStore();
        var key = "test-key-123";

        // Act
        var firstResult = await store.TryAddAsync(key);
        var secondResult = await store.TryAddAsync(key);

        // Assert
        Assert.True(firstResult);
        Assert.False(secondResult);
    }
}
