namespace Catalog.Tests;

using Catalog.Api.Contracts;
using Catalog.Api.Controllers;
using Catalog.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Unit tests verifying HTTP status codes and RFC 7807 responses on ProductsController.
/// 
/// WHY WE CREATED THIS:
/// Ensures controllers return the correct HTTP status codes (200 OK, 204 NoContent, 404 NotFound)
/// and RFC 7807 ProblemDetails objects without depending on an external web server or database.
/// 
/// CONCEPT: TESTING CONTROLLERS WITH FAKE SERVICE
/// Rather than introducing heavy external mocking libraries, a lightweight test fake implements
/// IProductService. This keeps test setup clear, fast, and completely decoupled from database internals.
/// </summary>
public class ProductsControllerTests
{
    private class FakeProductService : IProductService
    {
        public Func<Guid, Task<ProductResponse?>>? GetByIdHandler { get; set; }
        public Func<Guid, UpdateProductRequest, Task<ProductResponse?>>? UpdateHandler { get; set; }
        public Func<Guid, Task<bool>>? DeleteHandler { get; set; }
        public Func<CreateProductRequest, Task<ProductResponse>>? CreateHandler { get; set; }
        public Func<Task<IReadOnlyList<ProductResponse>>>? GetAllHandler { get; set; }

        public Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
            GetAllHandler != null ? GetAllHandler() : Task.FromResult<IReadOnlyList<ProductResponse>>(Array.Empty<ProductResponse>());

        public Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdHandler != null ? GetByIdHandler(id) : Task.FromResult<ProductResponse?>(null);

        public Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default) =>
            CreateHandler != null ? CreateHandler(request) : Task.FromResult(new ProductResponse(Guid.NewGuid(), request.Name, request.Description, request.Price, true, DateTime.UtcNow));

        public Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default) =>
            UpdateHandler != null ? UpdateHandler(id, request) : Task.FromResult<ProductResponse?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            DeleteHandler != null ? DeleteHandler(id) : Task.FromResult(false);
    }

    [Fact]
    public async Task GetById_WhenProductExists_Returns200OkWithProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedProduct = new ProductResponse(productId, "Desk Mat", "Leather desk mat", 29.99m, true, DateTime.UtcNow);
        var fakeService = new FakeProductService
        {
            GetByIdHandler = id => Task.FromResult<ProductResponse?>(expectedProduct)
        };
        var controller = new ProductsController(fakeService);

        // Act
        var result = await controller.GetById(productId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var product = Assert.IsType<ProductResponse>(okResult.Value);
        Assert.Equal(productId, product.Id);
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_Returns404NotFoundWithProblemDetails()
    {
        // Arrange
        var fakeService = new FakeProductService
        {
            GetByIdHandler = _ => Task.FromResult<ProductResponse?>(null)
        };
        var controller = new ProductsController(fakeService);
        var missingId = Guid.NewGuid();

        // Act
        var result = await controller.GetById(missingId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Product Not Found", problem.Title);
        Assert.Contains(missingId.ToString(), problem.Detail);
    }

    [Fact]
    public async Task Update_WhenProductExists_Returns200OkWithUpdatedProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequest("Standing Desk", "Electric adjustable", 450.00m, true);
        var updatedProduct = new ProductResponse(productId, request.Name, request.Description, request.Price, request.IsActive, DateTime.UtcNow);
        var fakeService = new FakeProductService
        {
            UpdateHandler = (id, req) => Task.FromResult<ProductResponse?>(updatedProduct)
        };
        var controller = new ProductsController(fakeService);

        // Act
        var result = await controller.Update(productId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var product = Assert.IsType<ProductResponse>(okResult.Value);
        Assert.Equal("Standing Desk", product.Name);
        Assert.Equal(450.00m, product.Price);
    }

    [Fact]
    public async Task Update_WhenProductDoesNotExist_Returns404NotFoundWithProblemDetails()
    {
        // Arrange
        var fakeService = new FakeProductService
        {
            UpdateHandler = (_, _) => Task.FromResult<ProductResponse?>(null)
        };
        var controller = new ProductsController(fakeService);
        var missingId = Guid.NewGuid();
        var request = new UpdateProductRequest("Ghost Product", "Does not exist", 10.00m, true);

        // Act
        var result = await controller.Update(missingId, request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Product Not Found", problem.Title);
    }

    [Fact]
    public async Task Delete_WhenProductExists_Returns204NoContent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fakeService = new FakeProductService
        {
            DeleteHandler = id => Task.FromResult(true)
        };
        var controller = new ProductsController(fakeService);

        // Act
        var result = await controller.Delete(productId, CancellationToken.None);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenProductDoesNotExist_Returns404NotFoundWithProblemDetails()
    {
        // Arrange
        var fakeService = new FakeProductService
        {
            DeleteHandler = _ => Task.FromResult(false)
        };
        var controller = new ProductsController(fakeService);
        var missingId = Guid.NewGuid();

        // Act
        var result = await controller.Delete(missingId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Product Not Found", problem.Title);
    }
}
