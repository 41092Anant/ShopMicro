namespace Order.Api.Clients;

using Order.Api.Contracts;

/// <summary>
/// Contract for synchronous HTTP client communication with the downstream Catalog microservice.
/// 
/// CONCEPT: SYNCHRONOUS INTER-SERVICE COMMUNICATION
/// In microservices, synchronous HTTP queries are used when immediate real-time data is required 
/// (e.g. validating product existence and current unit prices during order placement).
/// 
/// Passing CancellationToken guarantees that if the incoming order request is cancelled
/// or times out, downstream network calls and sockets abort cleanly without wasting resources.
/// </summary>
public interface ICatalogClient
{
    Task<CatalogProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
