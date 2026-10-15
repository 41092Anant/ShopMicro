namespace Catalog.Api.Services;

using Catalog.Api.Contracts;
using Catalog.Api.Data;
using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service implementation managing category business logic and PostgreSQL operations.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly CatalogDbContext _dbContext;

    public CategoryService(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description, c.CreatedUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return category is null ? null : new CategoryResponse(category.Id, category.Name, category.Description, category.CreatedUtc);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.Name, category.Description, category.CreatedUtc);
    }

    public async Task<IReadOnlyList<ProductResponse>?> GetProductsByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            return null;
        }

        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.IsActive, p.CreatedUtc))
            .ToListAsync(cancellationToken);
    }
}
