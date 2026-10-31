namespace Notification.Worker.Consumers;

using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

/// <summary>
/// MassTransit consumer that handles OrderCreatedEvent messages from RabbitMQ.
/// Simulates processing order confirmations and customer notifications asynchronously.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

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

        return Task.CompletedTask;
    }
}
