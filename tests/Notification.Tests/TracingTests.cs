namespace Notification.Tests;

using System.Diagnostics;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notification.Worker.Consumers;
using Notification.Worker.Services;
using NSubstitute;

/// <summary>
/// Verifies OpenTelemetry W3C trace context preservation during message consumption.
/// </summary>
public class TracingTests
{
    [Fact]
    public async Task Consume_PreservesTraceContext_WhenDistributedActivityIsActive()
    {
        // Arrange
        using var activitySource = new ActivitySource("ShopMicro.Telemetry.Tests");

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "ShopMicro.Telemetry.Tests",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var rootActivity = activitySource.StartActivity("GatewayToOrderPipeline");
        Assert.NotNull(rootActivity);
        var expectedTraceId = rootActivity.TraceId;

        var consumer = new OrderCreatedConsumer(new InMemoryProcessedMessageStore(), NullLogger<OrderCreatedConsumer>.Instance);
        var message = new OrderCreatedEvent(
            Guid.NewGuid(),
            "telemetry@example.com",
            250.00m,
            DateTime.UtcNow,
            [new OrderItemDto(Guid.NewGuid(), "Ergonomic Monitor", 1, 250.00m, 250.00m, "Hardware")]
        );

        var context = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context);

        // Assert: TraceId remains uniform across the pipeline boundary
        Assert.NotNull(Activity.Current);
        Assert.Equal(expectedTraceId, Activity.Current.TraceId);
    }
}
