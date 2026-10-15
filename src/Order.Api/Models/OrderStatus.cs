namespace Order.Api.Models;

/// <summary>
/// Represents the lifecycle status of an order.
/// </summary>
public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3
}
