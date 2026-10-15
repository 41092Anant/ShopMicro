namespace Order.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Api.Clients;
using Order.Api.Contracts;
using Order.Api.Data;
using Order.Api.Models;
using Order.Api.Services;

/// <summary>
/// Unit tests for OrderService verifying business logic, calculations, and Catalog integration.
/// </summary>
public class OrderServiceTests
{
    private static OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrderDbContext(options);
    }

    private class StubCatalogClient : ICatalogClient
    {
        private readonly Dictionary<Guid, CatalogProductDto> _products = new();

        public void AddProduct(CatalogProductDto product)
        {
            _products[product.Id] = product;
        }

        public Task<CatalogProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _products.TryGetValue(productId, out var product);
            return Task.FromResult(product);
        }
    }

    [Fact]
    public async Task CreateAsync_WhenProductsAreValid_CreatesOrderAndCalculatesTotals()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var stubCatalog = new StubCatalogClient();

        var product1 = new CatalogProductDto(Guid.NewGuid(), "Mechanical Keyboard", 100.00m, true, "Electronics");
        var product2 = new CatalogProductDto(Guid.NewGuid(), "Gaming Mouse", 50.00m, true, "Accessories");
        stubCatalog.AddProduct(product1);
        stubCatalog.AddProduct(product2);

        var service = new OrderService(dbContext, stubCatalog, NullLogger<OrderService>.Instance);

        var request = new CreateOrderRequest
        {
            CustomerEmail = "gamer@example.com",
            Items =
            [
                new CreateOrderItemRequest { ProductId = product1.Id, Quantity = 2 },
                new CreateOrderItemRequest { ProductId = product2.Id, Quantity = 3 }
            ]
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("gamer@example.com", result.CustomerEmail);
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal(350.00m, result.TotalAmount); // (100 * 2) + (50 * 3) = 350
        Assert.Equal(2, result.Items.Count);

        // Verify persistence in SQL Server context
        var savedOrder = await dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == result.Id);
        Assert.NotNull(savedOrder);
        Assert.Equal(350.00m, savedOrder.TotalAmount);
        Assert.Equal(2, savedOrder.Items.Count);

        var item1 = savedOrder.Items.First(i => i.ProductId == product1.Id);
        Assert.Equal("Mechanical Keyboard", item1.ProductName);
        Assert.Equal("Electronics", item1.CategoryName);
        Assert.Equal(100.00m, item1.UnitPrice);
        Assert.Equal(2, item1.Quantity);
        Assert.Equal(200.00m, item1.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_WhenProductNotFoundInCatalog_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var stubCatalog = new StubCatalogClient(); // Empty catalog
        var service = new OrderService(dbContext, stubCatalog, NullLogger<OrderService>.Instance);

        var missingProductId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            Items =
            [
                new CreateOrderItemRequest { ProductId = missingProductId, Quantity = 1 }
            ]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
        Assert.Contains(missingProductId.ToString(), exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenProductIsInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var stubCatalog = new StubCatalogClient();

        var inactiveProduct = new CatalogProductDto(Guid.NewGuid(), "Discontinued Item", 45.00m, false);
        stubCatalog.AddProduct(inactiveProduct);

        var service = new OrderService(dbContext, stubCatalog, NullLogger<OrderService>.Instance);

        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            Items =
            [
                new CreateOrderItemRequest { ProductId = inactiveProduct.Id, Quantity = 1 }
            ]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
        Assert.Contains("not available for purchase", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsOrderResponseWithItems()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = "alice@example.com",
            TotalAmount = 79.99m,
            Status = OrderStatus.Confirmed,
            CreatedUtc = DateTime.UtcNow,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    ProductName = "USB-C Cable",
                    UnitPrice = 79.99m,
                    Quantity = 1,
                    TotalPrice = 79.99m
                }
            ]
        };

        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var service = new OrderService(dbContext, new StubCatalogClient(), NullLogger<OrderService>.Instance);

        // Act
        var result = await service.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("alice@example.com", result.CustomerEmail);
        Assert.Equal(OrderStatus.Confirmed, result.Status);
        Assert.Single(result.Items);
        Assert.Equal("USB-C Cable", result.Items[0].ProductName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = new OrderService(dbContext, new StubCatalogClient(), NullLogger<OrderService>.Instance);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}
