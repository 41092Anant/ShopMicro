namespace Order.Api.Clients;

using System.Net;
using System.Net.Http.Json;
using Order.Api.Contracts;

/// <summary>
/// Typed HTTP client implementation for querying Catalog.Api.
/// 
/// CONCEPT: TYPED HTTP CLIENT LIFECYCLE
/// Typed clients are registered via builder.Services.AddHttpClient().
/// Under the hood, IHttpClientFactory manages the lifecycle of the underlying HttpMessageHandler pool,
/// preventing TCP socket exhaustion while enabling configuration of BaseAddress and Polly resilience pipelines.
/// </summary>
public class CatalogClient : ICatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(HttpClient httpClient, ILogger<CatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CatalogProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/products/{productId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Catalog product {ProductId} was not found.", productId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatalogProductDto>(cancellationToken);
    }
}
