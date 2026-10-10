namespace Catalog.Api.Models;

/// <summary>
/// Internal Entity Framework Core database entity representing a product in PostgreSQL.
/// 
/// WHY WE CREATED THIS:
/// This class represents the exact table schema and state inside the database.
/// 
/// CONCEPT: ENTITY VS DTO (DATA TRANSFER OBJECT)
/// - An 'Entity' (this class) represents data persistence and table relationships. It lives strictly inside Catalog.Api.
/// - A 'DTO' (in Contracts/) represents the shape of data exchanged over the network with callers.
/// - In microservices, NEVER share Entities outside the service boundary or accept them directly in controllers.
/// 
/// CONCEPT: GUID FOR PRIMARY KEYS
/// We use Guid (Globally Unique Identifier / UUID) instead of integer IDs (1, 2, 3...) so that any distributed
/// service or client can generate IDs independently without querying the database for the next sequential number.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
