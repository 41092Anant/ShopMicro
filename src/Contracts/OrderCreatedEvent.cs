namespace Contracts;

/// <summary>
/// Event contract published when an order has been successfully placed.
/// 
/// WHY WE CREATED THIS IN A SHARED CONTRACTS PROJECT:
/// Enforces the microservices boundary: services share message contracts (schema),
/// NOT internal EF entities, database logic, or private service models.
/// 
/// CONCEPT: EVENT-DRIVEN CONTRACTS
/// This record is immutable and represents a past fact ('OrderCreated').
/// Consumers like Notification.Worker or Analytics subscribe to this contract without
/// coupling to Order.Api internals.
/// </summary>
public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerEmail,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items
);

/// <summary>
/// Snapshot of an item within the created order event.
/// </summary>
public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? CategoryName
);
