namespace Catalog.Tests;

using Catalog.Api.Contracts;
using Catalog.Api.Data;
using Catalog.Api.Models;
using Catalog.Api.Services;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Unit tests for CategoryService business logic and EF Core operations.
/// </summary>
public class CategoryServiceTests
{
    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategoriesOrderedByName()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            new Category { Id = Guid.NewGuid(), Name = "Electronics" },
            new Category { Id = Guid.NewGuid(), Name = "Apparel" }
        );
        await dbContext.SaveChangesAsync();

        var service = new CategoryService(dbContext);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Apparel", result[0].Name);
        Assert.Equal("Electronics", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExists_ReturnsCategoryResponse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Books",
            Description = "Printed books and e-books"
        };
        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();

        var service = new CategoryService(dbContext);

        // Act
        var result = await service.GetByIdAsync(category.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Books", result.Name);
        Assert.Equal("Printed books and e-books", result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_PersistsCategoryAndReturnsResponse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);
        var request = new CreateCategoryRequest("Home & Kitchen", "Appliances and cookware");

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Home & Kitchen", result.Name);
        Assert.Equal("Appliances and cookware", result.Description);

        var saved = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Home & Kitchen", saved.Name);
    }

    [Fact]
    public async Task GetProductsByCategoryIdAsync_WhenCategoryExists_ReturnsMatchingProducts()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var categoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();

        dbContext.Categories.Add(new Category { Id = categoryId, Name = "Computers" });
        dbContext.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Laptop", Price = 999.99m, CategoryId = categoryId },
            new Product { Id = Guid.NewGuid(), Name = "Monitor", Price = 299.99m, CategoryId = categoryId },
            new Product { Id = Guid.NewGuid(), Name = "Desk", Price = 199.99m, CategoryId = otherCategoryId }
        );
        await dbContext.SaveChangesAsync();

        var service = new CategoryService(dbContext);

        // Act
        var products = await service.GetProductsByCategoryIdAsync(categoryId);

        // Assert
        Assert.NotNull(products);
        Assert.Equal(2, products.Count);
        Assert.Contains(products, p => p.Name == "Laptop");
        Assert.Contains(products, p => p.Name == "Monitor");
    }

    [Fact]
    public async Task GetProductsByCategoryIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        // Act
        var result = await service.GetProductsByCategoryIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}
