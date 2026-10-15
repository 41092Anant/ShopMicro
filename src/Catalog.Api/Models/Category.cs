namespace Catalog.Api.Models;

/// <summary>
/// Internal Entity Framework Core database entity representing a product category in PostgreSQL.
/// 
/// CONCEPT: DOMAIN TAXONOMY & ONE-TO-MANY RELATIONSHIPS
/// Categories organize products into logical groups (e.g. Electronics, Furniture, Books).
/// One Category has many Products (1-to-many relationship).
/// </summary>
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation property for products assigned to this category
    public List<Product> Products { get; set; } = [];
}
