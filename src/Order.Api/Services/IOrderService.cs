namespace Order.Api.Services;

using Order.Api.Contracts;

/// <summary>
/// Service contract defining business operations for customer orders.
/// 
/// CONCEPT: BUSINESS LOGIC ORCHESTRATION
/// Controllers remain lean by delegating HTTP payload handling to service classes.
/// The service orchestrates:
/// 1. Synchronous inter-service calls to Catalog.Api via ICatalogClient.
/// 2. Price calculation and historical snapshot creation.
/// 3. Atomic persistence of Order and OrderItem entities to SQL Server via OrderDbContext.
/// </summary>
public interface IOrderService
{
    Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
}
