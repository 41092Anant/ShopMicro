namespace Catalog.Api.Services;

using Catalog.Api.Contracts;

/// <summary>
/// Service contract defining business operations for catalog products.
/// 
/// WHY WE CREATED THIS:
/// 1. Separation of Concerns: Controllers handle HTTP concerns (status codes, routes, headers).
///    Services handle business logic and database interactions.
/// 2. Testability: By depending on an interface (IProductService) instead of a concrete class,
///    we can easily write unit tests for our Controllers in Phase 2 using mock services.
/// 
/// CONCEPT: ASYNCHRONOUS I/O (Task<T>)
/// Web servers have a limited pool of worker threads. When an operation waits on the database or network:
/// - Synchronous code blocks the thread, wasting server memory and scalability.
/// - Asynchronous code (Task / async / await) frees the thread to handle other incoming HTTP requests
///   while waiting for the database to respond.
/// 
/// CONCEPT: CANCELLATION TOKEN (CancellationToken)
/// In microservices, requests can be aborted by users, or timed out by upstream gateways (like YARP).
/// By propagating a CancellationToken through every method down to EF Core, the database query
/// is immediately aborted if the caller disconnects, saving CPU and database resources.
/// </summary>
public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
