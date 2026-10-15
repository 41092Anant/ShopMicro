namespace Order.Api.Contracts;

/// <summary>
/// Client-side Data Transfer Object (DTO) representing product details fetched from Catalog.Api.
/// 
/// CONCEPT: CONSUMER-DEFINED CONTRACT (NO SHARED EF ENTITIES)
/// In a monolithic application, Order code would directly reference 'Catalog.Api.Models.Product'
/// or inject CatalogDbContext. In microservices, services have zero compile-time references to
/// each other's database models or internal schemas.
/// 
/// BENEFITS OF CONSUMER-DEFINED CONTRACTS:
/// 1. Autonomous Evolution: Catalog.Api can add or rename private database columns without breaking Order.Api.
/// 2. Bounded Context Isolation: Order.Api only defines and binds the exact subset of fields it needs 
///    (Id, Name, Price, IsActive). It intentionally ignores Catalog internal metadata (Description, CreatedUtc).
/// 3. Loose Coupling: If Catalog.Api changes its internal storage from PostgreSQL to DocumentDB, Order.Api is unaffected.
/// </summary>
public record CatalogProductDto(
    Guid Id,
    string Name,
    decimal Price,
    bool IsActive
);
