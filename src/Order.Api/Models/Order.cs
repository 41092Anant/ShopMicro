namespace Order.Api.Models;

/// <summary>
/// Database entity representing a customer order in the Order microservice.
/// 
/// CONCEPT: MICROSERVICE AGGREGATE BOUNDARY
/// The Order entity acts as the aggregate root for an order and its items.
/// All monetary computations and status transitions are recorded within this service's boundary.
/// </summary>
public class Order
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation property for order line items
    public List<OrderItem> Items { get; set; } = [];
}
