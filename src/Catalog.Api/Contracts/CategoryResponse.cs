namespace Catalog.Api.Contracts;

/// <summary>
/// Public response DTO representing a category in the catalog.
/// </summary>
public record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedUtc
);
