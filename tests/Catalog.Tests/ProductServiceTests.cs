namespace Catalog.Tests;

using Catalog.Api.Contracts;
using Catalog.Api.Data;
using Catalog.Api.Models;
using Catalog.Api.Services;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Unit tests for ProductService business logic and EF Core database operations.
/// 
/// WHY WE CREATED THIS:
/// Verifies database interactions (queries, updates, deletions) in an isolated in-memory EF Core store.
/// 
/// CONCEPT: IN-MEMORY DATABASE ISOLATION
/// By generating a unique database name (Guid.NewGuid()) for each test, each test executes against
/// an empty, pristine database with no shared state or test pollution.
/// </summary>
public class ProductServiceTests
{
    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProductResponse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
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

        var service = new ProductService(dbContext);

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
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = new ProductService(dbContext);
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

        var service = new ProductService(dbContext);
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
        var service = new ProductService(dbContext);
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

        var service = new ProductService(dbContext);

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
        var service = new ProductService(dbContext);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }
}
