namespace Catalog.Api.Contracts;

/// <summary>
/// Public response DTO returned to API consumers representing a product.
/// 
/// WHY WE CREATED THIS:
/// 1. Information Hiding: If we add private or internal columns to our database table later
///    (e.g. RowVersion, InternalVendorNotes, CostPrice), they will NOT be leaked to the public API.
/// 2. Stable Contract: Other microservices (like Order.Api) rely on this shape. We can safely
///    refactor our database schema without breaking callers as long as this response contract stays stable.
/// 3. Read-Only Immutability: Uses 'record' to guarantee the response data cannot be mutated once constructed.
/// </summary>
public record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    bool IsActive,
    DateTime CreatedUtc,
    Guid? CategoryId = null,
    string? CategoryName = null
);
