namespace Order.Api.Services;

using Microsoft.EntityFrameworkCore;
using Order.Api.Clients;
using Order.Api.Contracts;
using Order.Api.Data;
using Order.Api.Models;

/// <summary>
/// Service implementation managing customer orders and coordinating with Catalog.Api.
/// 
/// CONCEPT: HISTORICAL DATA SNAPSHOTTING
/// When creating an order line item, we fetch the current product price and name from Catalog.Api
/// and store them as permanent, immutable snapshots inside SQL Server's OrderItems table.
/// Even if Catalog changes prices tomorrow, past orders retain their financial integrity.
/// </summary>
public class OrderService : IOrderService
{
    private readonly OrderDbContext _dbContext;
    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(OrderDbContext dbContext, ICatalogClient catalogClient, ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _catalogClient = catalogClient;
        _logger = logger;
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : MapToResponse(order);
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new order for customer {CustomerEmail} with {ItemCount} items.",
            request.CustomerEmail, request.Items.Count);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };

        decimal totalAmount = 0m;

        foreach (var item in request.Items)
        {
            // Call Catalog.Api via typed resilient HTTP client
            var product = await _catalogClient.GetProductByIdAsync(item.ProductId, cancellationToken);

            if (product is null)
            {
                _logger.LogWarning("Order creation rejected: Product {ProductId} not found in Catalog.", item.ProductId);
                throw new InvalidOperationException($"Product with ID '{item.ProductId}' was not found in Catalog.");
            }

            if (!product.IsActive)
            {
                _logger.LogWarning("Order creation rejected: Product {ProductId} ({ProductName}) is inactive.",
                    product.Id, product.Name);
                throw new InvalidOperationException($"Product '{product.Name}' ({product.Id}) is not available for purchase.");
            }

            var lineTotal = product.Price * item.Quantity;
            totalAmount += lineTotal;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                TotalPrice = lineTotal,
                Order = order
            });
        }

        order.TotalAmount = totalAmount;

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} successfully placed for customer {CustomerEmail} totaling {TotalAmount:C}.",
            order.Id, order.CustomerEmail, order.TotalAmount);

        return MapToResponse(order);
    }

    private static OrderResponse MapToResponse(Order order) => new()
    {
        Id = order.Id,
        CustomerEmail = order.CustomerEmail,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        CreatedUtc = order.CreatedUtc,
        Items = order.Items.Select(i => new OrderItemResponse
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            TotalPrice = i.TotalPrice
        }).ToList()
    };
}
