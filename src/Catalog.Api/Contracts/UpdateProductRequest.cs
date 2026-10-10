namespace Catalog.Api.Contracts;

using System.ComponentModel.DataAnnotations;

public record UpdateProductRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(1000)] string? Description,
    [Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than zero.")] decimal Price,
    bool IsActive
);
