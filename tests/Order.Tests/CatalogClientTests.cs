namespace Order.Tests;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Api.Clients;
using Order.Api.Contracts;

/// <summary>
/// Unit tests for CatalogClient verifying HTTP handling, JSON deserialization, 404 handling, and cancellation.
/// </summary>
public class CatalogClientTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenProductExists_ReturnsCatalogProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedProduct = new CatalogProductDto(productId, "Ergonomic Chair", 299.99m, true);

        var handler = new MockHttpMessageHandler(request =>
        {
            Assert.Equal($"/products/{productId}", request.RequestUri?.PathAndQuery);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedProduct),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };
        var client = new CatalogClient(httpClient, NullLogger<CatalogClient>.Instance);

        // Act
        var result = await client.GetProductByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedProduct.Id, result.Id);
        Assert.Equal(expectedProduct.Name, result.Name);
        Assert.Equal(expectedProduct.Price, result.Price);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenProductNotFound_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };
        var client = new CatalogClient(httpClient, NullLogger<CatalogClient>.Instance);

        // Act
        var result = await client.GetProductByIdAsync(productId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenServerError_ThrowsHttpRequestException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };
        var client = new CatalogClient(httpClient, NullLogger<CatalogClient>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetProductByIdAsync(productId));
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };
        var client = new CatalogClient(httpClient, NullLogger<CatalogClient>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetProductByIdAsync(productId, cts.Token));
    }
}
