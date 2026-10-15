namespace Order.Api.Data;

using Microsoft.EntityFrameworkCore;
using Order.Api.Models;

/// <summary>
/// Entity Framework Core database context for the Order microservice.
/// 
/// WHY WE CREATED THIS:
/// Manages database access for Order entities and their line items, configured for Microsoft SQL Server.
/// 
/// CONCEPT: ENUM TO STRING CONVERSION
/// Storing enum values as strings (e.g. 'Pending', 'Confirmed') rather than integers in SQL tables
/// makes database inspections human-readable and prevents data corruption if enum integer orders are modified.
/// 
/// CONCEPT: CASCADE DELETION
/// Configuring cascade deletion between Order and OrderItem guarantees that removing an order automatically
/// cleans up its associated line items, preserving database referential integrity.
/// </summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasMany(e => e.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
        });
    }
}
