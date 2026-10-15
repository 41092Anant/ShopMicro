namespace Order.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Api.Controllers;
using Order.Api.Contracts;
using Order.Api.Models;
using Order.Api.Services;

/// <summary>
/// Unit tests for OrdersController verifying HTTP response codes and RFC 7807 problem details.
/// </summary>
public class OrdersControllerTests
{
    private class StubOrderService : IOrderService
    {
        public Func<Guid, CancellationToken, Task<OrderResponse?>>? GetByIdHandler { get; set; }
        public Func<CreateOrderRequest, CancellationToken, Task<OrderResponse>>? CreateHandler { get; set; }

        public Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => GetByIdHandler != null ? GetByIdHandler(id, cancellationToken) : Task.FromResult<OrderResponse?>(null);

        public Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
            => CreateHandler != null ? CreateHandler(request, cancellationToken) : Task.FromResult(new OrderResponse());
    }

    [Fact]
    public async Task GetById_WhenFound_Returns200OkWithOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new OrderResponse
        {
            Id = orderId,
            CustomerEmail = "test@example.com",
            TotalAmount = 150.00m,
            Status = OrderStatus.Pending
        };

        var stubService = new StubOrderService
        {
            GetByIdHandler = (id, _) => Task.FromResult<OrderResponse?>(expectedOrder)
        };

        var controller = new OrdersController(stubService, NullLogger<OrdersController>.Instance);

        // Act
        var result = await controller.GetById(orderId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var order = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.Equal(orderId, order.Id);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404NotFoundWithProblemDetails()
    {
        // Arrange
        var stubService = new StubOrderService
        {
            GetByIdHandler = (_, _) => Task.FromResult<OrderResponse?>(null)
        };

        var controller = new OrdersController(stubService, NullLogger<OrdersController>.Instance);

        // Act
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task Create_WhenSuccessful_Returns201CreatedAtAction()
    {
        // Arrange
        var newOrderId = Guid.NewGuid();
        var createdOrder = new OrderResponse
        {
            Id = newOrderId,
            CustomerEmail = "buyer@example.com",
            TotalAmount = 199.99m,
            Status = OrderStatus.Pending
        };

        var stubService = new StubOrderService
        {
            CreateHandler = (_, _) => Task.FromResult(createdOrder)
        };

        var controller = new OrdersController(stubService, NullLogger<OrdersController>.Instance);
        var request = new CreateOrderRequest
        {
            CustomerEmail = "buyer@example.com",
            Items = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
        };

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(OrdersController.GetById), createdResult.ActionName);
        var body = Assert.IsType<OrderResponse>(createdResult.Value);
        Assert.Equal(newOrderId, body.Id);
    }

    [Fact]
    public async Task Create_WhenInvalidOperationException_Returns400BadRequestWithProblemDetails()
    {
        // Arrange
        var stubService = new StubOrderService
        {
            CreateHandler = (_, _) => throw new InvalidOperationException("Product was not found in Catalog.")
        };

        var controller = new OrdersController(stubService, NullLogger<OrdersController>.Instance);
        var request = new CreateOrderRequest
        {
            CustomerEmail = "buyer@example.com",
            Items = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
        };

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Product was not found in Catalog.", problem.Detail);
    }

    [Fact]
    public async Task Create_WhenHttpRequestException_Returns503ServiceUnavailableWithProblemDetails()
    {
        // Arrange
        var stubService = new StubOrderService
        {
            CreateHandler = (_, _) => throw new HttpRequestException("Catalog connection refused")
        };

        var controller = new OrdersController(stubService, NullLogger<OrdersController>.Instance);
        var request = new CreateOrderRequest
        {
            CustomerEmail = "buyer@example.com",
            Items = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
        };

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, errorResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(errorResult.Value);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.Status);
    }

    [Fact]
    public async Task Create_WhenDownstreamTimeout_Returns504GatewayTimeoutWithProblemDetails()
    {
        // Arrange
        var stubService = new StubOrderService
        {
            CreateHandler = (_, _) => throw new TimeoutException("Downstream timeout")
                // When HttpClient throws TaskCanceledException due to timeout, it's an OperationCanceledException
        };

        // Simulate OperationCanceledException thrown by HttpClient timeout (not by caller's token)
        var stubServiceWithTimeout = new StubOrderService
        {
            CreateHandler = (_, _) => Task.FromException<OrderResponse>(new OperationCanceledException("HttpClient timeout"))
        };

        var controller = new OrdersController(stubServiceWithTimeout, NullLogger<OrdersController>.Instance);
        var request = new CreateOrderRequest
        {
            CustomerEmail = "buyer@example.com",
            Items = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
        };

        // Act (caller token is CancellationToken.None, so caller did NOT request cancellation)
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var timeoutResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, timeoutResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(timeoutResult.Value);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, problem.Status);
        Assert.Equal("Catalog Service Timeout", problem.Title);
    }
}
