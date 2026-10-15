namespace Catalog.Api.Contracts;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO (Data Transfer Object) representing the payload required to create a new product.
/// 
/// WHY WE CREATED THIS:
/// 1. Security (Over-posting protection): Clients should only submit product details (Name, Description, Price).
///    They must NEVER submit server-managed fields like 'Id' or 'CreatedUtc'.
/// 2. Validation: We enforce business rules at the API boundary before any database operations occur.
/// 
/// CONCEPT: C# RECORD
/// A 'record' is a special reference type introduced in modern C# primarily used for data containers.
/// - Immutability: Once created, properties cannot be changed, preventing accidental data modification.
/// - Value-based equality: Two record instances with identical property values are considered equal.
/// - Concise syntax: The positional syntax below automatically generates a constructor and read-only properties.
/// </summary>
public record CreateProductRequest(
    [property: Required(ErrorMessage = "Product name is required.")]
    [property: StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
    string Name,

    [property: StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    string? Description,

    [property: Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than zero.")]
    decimal Price,

    Guid? CategoryId = null
);
