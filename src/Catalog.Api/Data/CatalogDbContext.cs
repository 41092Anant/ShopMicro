namespace Catalog.Api.Data;

using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core database context for the Catalog microservice.
/// 
/// WHY WE CREATED THIS:
/// The DbContext is the central bridge between our C# application and the PostgreSQL database.
/// It coordinates querying, saving changes, and mapping C# objects to SQL tables.
/// 
/// CONCEPT: FLUENT API (OnModelCreating)
/// EF Core provides two ways to configure database schemas:
/// 1. Data Annotations (attributes on the entity class like [Required], [MaxLength]).
/// 2. Fluent API (methods inside OnModelCreating).
/// Fluent API is preferred in modern architectures because it keeps domain entities clean and POCO
/// (Plain Old CLR Objects) without tightly coupling them to database-specific configurations.
/// </summary>
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasMany(e => e.Products)
                  .WithOne(p => p.Category)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}
