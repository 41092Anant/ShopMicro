namespace Notification.Worker.Consumers;

using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using Notification.Worker.Services;

/// <summary>
/// MassTransit consumer that handles OrderCreatedEvent messages from RabbitMQ.
/// Implements idempotent message processing to guard against duplicate deliveries.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IProcessedMessageStore _idempotencyStore;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        IProcessedMessageStore idempotencyStore,
        ILogger<OrderCreatedConsumer> logger)
    {
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        var messageKey = $"order-{message.OrderId}";

        // Idempotency check: guard against duplicate message delivery from RabbitMQ
        var isNewMessage = await _idempotencyStore.TryAddAsync(messageKey, context.CancellationToken);
        if (!isNewMessage)
        {
            _logger.LogWarning(
                "Duplicate OrderCreatedEvent detected for OrderId={OrderId}. Skipping processing to preserve idempotency.",
                message.OrderId);
            return;
        }

        _logger.LogInformation(
            "Received OrderCreatedEvent: OrderId={OrderId}, CustomerEmail={CustomerEmail}, TotalAmount={TotalAmount:C}, ItemsCount={ItemsCount}",
            message.OrderId,
            message.CustomerEmail,
            message.TotalAmount,
            message.Items.Count);

        // Simulate sending an order confirmation email to the customer
        _logger.LogInformation(
            "Dispatching order confirmation notification to {CustomerEmail} for Order #{OrderId} (Total: {TotalAmount:C})",
            message.CustomerEmail,
            message.OrderId,
            message.TotalAmount);
    }
}
