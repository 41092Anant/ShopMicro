namespace Order.Api.Models;

/// <summary>
/// Database entity representing an individual item line within a customer order.
/// 
/// CONCEPT: NO SHARED EF ENTITIES ACROSS SERVICES (DATABASE-PER-SERVICE)
/// In a monolithic application, OrderItem would have a foreign key navigation property 'public Product Product { get; set; }'.
/// In microservices, services own independent databases. Order API has zero access to Catalog API's database.
/// Therefore, OrderItem stores:
/// 1. 'ProductId' (Guid) - A scalar identifier reference to the Catalog product.
/// 2. 'ProductName' & 'UnitPrice' - An immutable historical snapshot captured at the exact moment of purchase.
///    If the catalog price changes tomorrow, historical orders remain financially accurate.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }

    // Navigation property back to parent Order
    public Order Order { get; set; } = null!;
}
