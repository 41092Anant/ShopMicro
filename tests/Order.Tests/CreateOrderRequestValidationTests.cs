namespace Order.Tests;

using System.ComponentModel.DataAnnotations;
using Order.Api.Contracts;

/// <summary>
/// Unit tests verifying data annotation validation rules on CreateOrderRequest and CreateOrderItemRequest DTOs.
/// </summary>
public class CreateOrderRequestValidationTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);
        return validationResults;
    }

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2
                }
            ]
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missingatsign.com")]
    public void InvalidCustomerEmail_ShouldFailValidation(string invalidEmail)
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerEmail = invalidEmail,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderRequest.CustomerEmail)));
    }

    [Fact]
    public void EmptyItemsList_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            Items = []
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderRequest.Items)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void InvalidQuantity_ShouldFailValidation(int invalidQuantity)
    {
        // Arrange
        var itemRequest = new CreateOrderItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = invalidQuantity
        };

        // Act
        var results = ValidateModel(itemRequest);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderItemRequest.Quantity)));
    }

    [Fact]
    public void ValidItem_ShouldPassValidation()
    {
        // Arrange
        var itemRequest = new CreateOrderItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 5
        };

        // Act
        var results = ValidateModel(itemRequest);

        // Assert
        Assert.Empty(results);
    }
}
