namespace Catalog.Api.Services;

using System.Text.Json;
using Catalog.Api.Contracts;
using Catalog.Api.Data;
using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

/// <summary>
/// Implementation of IProductService handling product business logic, EF Core database operations,
/// and distributed cache caching (Cache-Aside pattern).
/// 
/// WHY WE CREATED THIS:
/// Encapsulates product retrieval, persistence, and caching so controllers stay thin and focused.
/// 
/// CONCEPT: CACHE-ASIDE PATTERN
/// Read requests first check the distributed cache (Redis) via IDistributedCache.
/// - Cache hit: return the deserialized object directly (~sub-millisecond latency).
/// - Cache miss: query the relational store (PostgreSQL via EF Core), populate the cache with a TTL,
///   and return the data to the caller.
/// </summary>
public class ProductService : IProductService
{
    private readonly CatalogDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public ProductService(CatalogDbContext dbContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(MapToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:{id}";

        // 1. Attempt cache lookup (Cache-Aside)
        try
        {
            var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                var cachedProduct = JsonSerializer.Deserialize<ProductResponse>(cachedJson, JsonOptions);
                if (cachedProduct is not null)
                {
                    return cachedProduct;
                }
            }
        }
        catch
        {
            // Transient cache failure: fall back to primary database query
        }

        // 2. Cache miss: Query PostgreSQL via EF Core
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var response = MapToResponse(product);

        // 3. Populate cache with TTL (5 minutes) for subsequent requests
        try
        {
            var serialized = JsonSerializer.Serialize(response, JsonOptions);
            await _cache.SetStringAsync(cacheKey, serialized, CacheOptions, cancellationToken);
        }
        catch
        {
            // Cache write failure: return retrieved data without failing the request
        }

        return response;
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
            CreatedUtc = DateTime.UtcNow,
            CategoryId = request.CategoryId
        };

        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (product.CategoryId.HasValue)
        {
            await _dbContext.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);
        }

        var response = MapToResponse(product);

        // Cache pre-warming: write the newly created product to cache
        try
        {
            var serialized = JsonSerializer.Serialize(response, JsonOptions);
            await _cache.SetStringAsync($"product:{product.Id}", serialized, CacheOptions, cancellationToken);
        }
        catch
        {
            // Cache write failure: return response without failing the request
        }

        return response;
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Cache invalidation: evict stale cache entry so subsequent reads fetch fresh data
        try
        {
            await _cache.RemoveAsync($"product:{id}", cancellationToken);
        }
        catch
        {
            // Cache eviction failure: do not block returning updated entity
        }

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

        // Cache invalidation: evict deleted product from cache
        try
        {
            await _cache.RemoveAsync($"product:{id}", cancellationToken);
        }
        catch
        {
            // Cache eviction failure: do not block returning success
        }

        return true;
    }

    private static ProductResponse MapToResponse(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.IsActive,
            product.CreatedUtc,
            product.CategoryId,
            product.Category?.Name
        );
}
