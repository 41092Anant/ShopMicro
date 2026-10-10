namespace Catalog.Api.Contracts;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO representing the payload required to update an existing product.
/// 
/// WHY WE CREATED THIS SEPARATE FROM CREATE REQUEST:
/// While creating a product and updating a product look similar, they have different business rules:
/// - Create: Always sets IsActive = true automatically upon creation.
/// - Update: Allows the user/admin to toggle 'IsActive' (e.g. deactivate a discontinued product).
/// Having separate DTOs keeps API contracts explicit and prevents unintentional state changes.
/// </summary>
public record UpdateProductRequest(
    [property: Required(ErrorMessage = "Product name is required.")]
    [property: StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
    string Name,

    [property: StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    string? Description,

    [property: Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than zero.")]
    decimal Price,

    bool IsActive
);
