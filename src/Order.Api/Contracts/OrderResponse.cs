namespace Order.Api.Contracts;

using Order.Api.Models;

/// <summary>
/// Public response DTO representing an order returned by Order.Api.
/// </summary>
public class OrderResponse
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];
}

/// <summary>
/// Public response DTO representing an individual line item within an order response.
/// </summary>
public class OrderItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}
