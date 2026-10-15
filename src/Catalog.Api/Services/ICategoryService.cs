namespace Catalog.Api.Services;

using Catalog.Api.Contracts;

/// <summary>
/// Service contract defining business operations for product categories.
/// </summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResponse>?> GetProductsByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
