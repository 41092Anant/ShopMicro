namespace Catalog.Tests;

using Catalog.Api.Controllers;
using Catalog.Api.Contracts;
using Catalog.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Unit tests for CategoriesController verifying HTTP responses and RFC 7807 error details.
/// </summary>
public class CategoriesControllerTests
{
    private class StubCategoryService : ICategoryService
    {
        public Func<CancellationToken, Task<IReadOnlyList<CategoryResponse>>>? GetAllHandler { get; set; }
        public Func<Guid, CancellationToken, Task<CategoryResponse?>>? GetByIdHandler { get; set; }
        public Func<CreateCategoryRequest, CancellationToken, Task<CategoryResponse>>? CreateHandler { get; set; }
        public Func<Guid, CancellationToken, Task<IReadOnlyList<ProductResponse>?>>? GetProductsHandler { get; set; }

        public Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
            => GetAllHandler != null ? GetAllHandler(cancellationToken) : Task.FromResult<IReadOnlyList<CategoryResponse>>([]);

        public Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => GetByIdHandler != null ? GetByIdHandler(id, cancellationToken) : Task.FromResult<CategoryResponse?>(null);

        public Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
            => CreateHandler != null ? CreateHandler(request, cancellationToken) : Task.FromResult(new CategoryResponse(Guid.NewGuid(), "", null, DateTime.UtcNow));

        public Task<IReadOnlyList<ProductResponse>?> GetProductsByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
            => GetProductsHandler != null ? GetProductsHandler(categoryId, cancellationToken) : Task.FromResult<IReadOnlyList<ProductResponse>?>(null);
    }

    [Fact]
    public async Task GetAll_Returns200OkWithCategories()
    {
        // Arrange
        var expected = new List<CategoryResponse>
        {
            new(Guid.NewGuid(), "Electronics", "Gadgets", DateTime.UtcNow)
        };
        var stub = new StubCategoryService { GetAllHandler = _ => Task.FromResult<IReadOnlyList<CategoryResponse>>(expected) };
        var controller = new CategoriesController(stub);

        // Act
        var result = await controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var categories = Assert.IsAssignableFrom<IReadOnlyList<CategoryResponse>>(okResult.Value);
        Assert.Single(categories);
    }

    [Fact]
    public async Task GetById_WhenFound_Returns200OkWithCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var expected = new CategoryResponse(categoryId, "Books", "Reading materials", DateTime.UtcNow);
        var stub = new StubCategoryService { GetByIdHandler = (id, _) => Task.FromResult<CategoryResponse?>(expected) };
        var controller = new CategoriesController(stub);

        // Act
        var result = await controller.GetById(categoryId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var category = Assert.IsType<CategoryResponse>(okResult.Value);
        Assert.Equal(categoryId, category.Id);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404NotFoundWithProblemDetails()
    {
        // Arrange
        var stub = new StubCategoryService { GetByIdHandler = (_, _) => Task.FromResult<CategoryResponse?>(null) };
        var controller = new CategoriesController(stub);

        // Act
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task Create_WhenValid_Returns201CreatedAtAction()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var created = new CategoryResponse(categoryId, "Furniture", "Desks and chairs", DateTime.UtcNow);
        var stub = new StubCategoryService { CreateHandler = (_, _) => Task.FromResult(created) };
        var controller = new CategoriesController(stub);
        var request = new CreateCategoryRequest("Furniture", "Desks and chairs");

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(CategoriesController.GetById), createdResult.ActionName);
        var body = Assert.IsType<CategoryResponse>(createdResult.Value);
        Assert.Equal(categoryId, body.Id);
    }

    [Fact]
    public async Task GetProductsByCategory_WhenFound_Returns200OkWithProducts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var products = new List<ProductResponse>
        {
            new(Guid.NewGuid(), "Standing Desk", "Adjustable height", 399.99m, true, DateTime.UtcNow)
        };
        var stub = new StubCategoryService { GetProductsHandler = (_, _) => Task.FromResult<IReadOnlyList<ProductResponse>?>(products) };
        var controller = new CategoriesController(stub);

        // Act
        var result = await controller.GetProductsByCategory(categoryId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProducts = Assert.IsAssignableFrom<IReadOnlyList<ProductResponse>>(okResult.Value);
        Assert.Single(returnedProducts);
    }

    [Fact]
    public async Task GetProductsByCategory_WhenCategoryNotFound_Returns404NotFound()
    {
        // Arrange
        var stub = new StubCategoryService { GetProductsHandler = (_, _) => Task.FromResult<IReadOnlyList<ProductResponse>?>(null) };
        var controller = new CategoriesController(stub);

        // Act
        var result = await controller.GetProductsByCategory(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }
}
