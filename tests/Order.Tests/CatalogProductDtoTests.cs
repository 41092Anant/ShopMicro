namespace Order.Tests;

using Order.Api.Contracts;

public class CatalogProductDtoTests
{
    [Fact]
    public void CatalogProductDto_ShouldStoreProductData_WithoutSharedDependencies()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Mechanical Keyboard";
        var price = 129.99m;
        var isActive = true;

        // Act
        var dto = new CatalogProductDto(productId, productName, price, isActive);

        // Assert
        Assert.Equal(productId, dto.Id);
        Assert.Equal(productName, dto.Name);
        Assert.Equal(price, dto.Price);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void CatalogProductDto_Equality_ShouldBeValueBased()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto1 = new CatalogProductDto(id, "Mouse", 49.99m, true);
        var dto2 = new CatalogProductDto(id, "Mouse", 49.99m, true);

        // Assert: Records provide value-based equality out-of-the-box
        Assert.Equal(dto1, dto2);
    }
}
