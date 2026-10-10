namespace Catalog.Api.Services;

using Catalog.Api.Contracts;
using Catalog.Api.Data;
using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementation of IProductService handling product business logic and EF Core database operations.
/// 
/// WHY WE CREATED THIS:
/// This class encapsulates data access and business rules so our controllers remain thin and focused.
/// 
/// CONCEPT: DEPENDENCY INJECTION (DI)
/// Rather than creating 'new CatalogDbContext()' directly, the service receives it through constructor
/// injection. ASP.NET Core manages the lifetime of CatalogDbContext (Scoped = one per HTTP request).
/// 
/// CONCEPT: EF CORE AsNoTracking()
/// By default, EF Core tracks entities in memory so it can detect changes when SaveChangesAsync() is called.
/// For read-only operations (like GetAllAsync and GetByIdAsync), 'AsNoTracking()' disables change tracking,
/// which significantly reduces memory usage and speeds up query execution.
/// 
/// CONCEPT: DTO MAPPING (MapToResponse)
/// We manually map the internal Product entity to the public ProductResponse record. This keeps
/// database models decoupled from public contracts without needing heavy third-party mapping libraries.
/// </summary>
public class ProductService : IProductService
{
    private readonly CatalogDbContext _dbContext;

    public ProductService(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(MapToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static ProductResponse MapToResponse(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.IsActive,
            product.CreatedUtc
        );
}
