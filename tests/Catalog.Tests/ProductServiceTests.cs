namespace Catalog.Tests;

using System.Text.Json;
using Catalog.Api.Contracts;
using Catalog.Api.Data;
using Catalog.Api.Models;
using Catalog.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

/// <summary>
/// Unit tests for ProductService business logic, EF Core database operations,
/// and Cache-Aside distributed caching behavior.
/// 
/// WHY WE CREATED THIS:
/// Verifies database interactions and distributed cache lookups/populations in isolation
/// without requiring an external PostgreSQL instance or Redis server.
/// </summary>
public class ProductServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    private static IDistributedCache CreateDistributedCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProductResponse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Ergonomic Chair",
            Description = "Mesh high-back chair with lumbar support",
            Price = 249.99m,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };
        await dbContext.Products.AddAsync(existingProduct);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext, cache);

        // Act
        var result = await service.GetByIdAsync(existingProduct.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingProduct.Id, result.Id);
        Assert.Equal("Ergonomic Chair", result.Name);
        Assert.Equal(249.99m, result.Price);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_FetchesFromDbAndPopulatesCache()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mechanical Keyboard",
            Description = "Hot-swappable tactile switches",
            Price = 89.99m,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };
        await dbContext.Products.AddAsync(existingProduct);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext, cache);

        // Act
        var result = await service.GetByIdAsync(existingProduct.Id);

        // Assert - fetched correctly from database
        Assert.NotNull(result);
        Assert.Equal(existingProduct.Id, result.Id);
        Assert.Equal("Mechanical Keyboard", result.Name);

        // Assert - populated into distributed cache with product:id key
        var cachedJson = await cache.GetStringAsync($"product:{existingProduct.Id}");
        Assert.NotNull(cachedJson);
        var cachedProduct = JsonSerializer.Deserialize<ProductResponse>(cachedJson, JsonOptions);
        Assert.NotNull(cachedProduct);
        Assert.Equal(existingProduct.Id, cachedProduct.Id);
        Assert.Equal("Mechanical Keyboard", cachedProduct.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheHit_ReturnsCachedProductWithoutQueryingDb()
    {
        // Arrange - Database is completely empty, cache holds the item
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var productId = Guid.NewGuid();
        var cachedResponse = new ProductResponse(
            productId,
            "Cached Ultra-Wide Monitor",
            "Retrieved directly from Redis cache",
            499.99m,
            true,
            DateTime.UtcNow,
            null,
            null
        );
        var serialized = JsonSerializer.Serialize(cachedResponse, JsonOptions);
        await cache.SetStringAsync($"product:{productId}", serialized);

        var service = new ProductService(dbContext, cache);

        // Act
        var result = await service.GetByIdAsync(productId);

        // Assert - returned cached entity even though DB has no record
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Cached Ultra-Wide Monitor", result.Name);
        Assert.Equal(499.99m, result.Price);

        // Confirm DB remains untouched and empty
        Assert.Empty(await dbContext.Products.ToListAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var service = new ProductService(dbContext, cache);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductExists_UpdatesFieldsAndReturnsProductResponse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Original Monitor",
            Description = "1080p 60Hz",
            Price = 120.00m,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };
        await dbContext.Products.AddAsync(existingProduct);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext, cache);
        var updateRequest = new UpdateProductRequest(
            Name: "Updated 4K Monitor",
            Description: "4K UHD 144Hz IPS display",
            Price: 399.99m,
            IsActive: false);

        // Act
        var result = await service.UpdateAsync(existingProduct.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated 4K Monitor", result.Name);
        Assert.Equal("4K UHD 144Hz IPS display", result.Description);
        Assert.Equal(399.99m, result.Price);
        Assert.False(result.IsActive);

        // Verify persistence in database
        var persisted = await dbContext.Products.FindAsync(existingProduct.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Updated 4K Monitor", persisted.Name);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var service = new ProductService(dbContext, cache);
        var updateRequest = new UpdateProductRequest("Ghost Item", "No desc", 10.00m, true);

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), updateRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_RemovesFromDatabaseAndReturnsTrue()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Item To Delete",
            Description = "Will be removed",
            Price = 19.99m,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };
        await dbContext.Products.AddAsync(existingProduct);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext, cache);

        // Act
        var result = await service.DeleteAsync(existingProduct.Id);

        // Assert
        Assert.True(result);

        // Verify entity was removed from database
        var inDb = await dbContext.Products.FindAsync(existingProduct.Id);
        Assert.Null(inDb);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var service = new ProductService(dbContext, cache);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateAsync_PreWarmsCacheWithNewProduct()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var service = new ProductService(dbContext, cache);
        var request = new CreateProductRequest("Gaming Mouse", "Wireless 26K DPI optical sensor", 79.99m, null);

        // Act
        var result = await service.CreateAsync(request);

        // Assert - product created in DB
        Assert.NotNull(result);
        Assert.Equal("Gaming Mouse", result.Name);

        // Assert - cache pre-warmed
        var cachedJson = await cache.GetStringAsync($"product:{result.Id}");
        Assert.NotNull(cachedJson);
        var cached = JsonSerializer.Deserialize<ProductResponse>(cachedJson, JsonOptions);
        Assert.NotNull(cached);
        Assert.Equal(result.Id, cached.Id);
        Assert.Equal("Gaming Mouse", cached.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductExists_EvictsCachedEntry()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Monitor",
            Description = "1080p 60Hz",
            Price = 100.00m,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };
        await dbContext.Products.AddAsync(existingProduct);
        await dbContext.SaveChangesAsync();

        // Pre-populate cache with old data
        var oldCachedResponse = new ProductResponse(existingProduct.Id, "Old Monitor", "1080p 60Hz", 100.00m, true, DateTime.UtcNow);
        await cache.SetStringAsync($"product:{existingProduct.Id}", JsonSerializer.Serialize(oldCachedResponse, JsonOptions));

        var service = new ProductService(dbContext, cache);
        var updateRequest = new UpdateProductRequest("New OLED Monitor", "4K OLED 240Hz", 899.99m, true);

        // Act
        var result = await service.UpdateAsync(existingProduct.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New OLED Monitor", result.Name);

        // Assert - stale cache entry must be evicted (null)
        var cachedAfterUpdate = await cache.GetStringAsync($"product:{existingProduct.Id}");
        Assert.Null(cachedAfterUpdate);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_EvictsCachedEntry()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var cache = CreateDistributedCache();
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Item To Be Deleted",
            Description = "Temporary",
            Price = 15.00m,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };
        await dbContext.Products.AddAsync(existingProduct);
        await dbContext.SaveChangesAsync();

        // Pre-populate cache with item
        var cachedResponse = new ProductResponse(existingProduct.Id, "Item To Be Deleted", "Temporary", 15.00m, true, DateTime.UtcNow);
        await cache.SetStringAsync($"product:{existingProduct.Id}", JsonSerializer.Serialize(cachedResponse, JsonOptions));

        var service = new ProductService(dbContext, cache);

        // Act
        var result = await service.DeleteAsync(existingProduct.Id);

        // Assert
        Assert.True(result);

        // Assert - cache entry must be evicted (null)
        var cachedAfterDelete = await cache.GetStringAsync($"product:{existingProduct.Id}");
        Assert.Null(cachedAfterDelete);
    }
}
