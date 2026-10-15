namespace Catalog.Api.Contracts;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Public request DTO for creating a new product category.
/// </summary>
public record CreateCategoryRequest(
    [property: Required(ErrorMessage = "Category name is required.")]
    [property: StringLength(100, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 100 characters.")]
    string Name,

    [property: StringLength(500, ErrorMessage = "Category description cannot exceed 500 characters.")]
    string? Description = null
);
