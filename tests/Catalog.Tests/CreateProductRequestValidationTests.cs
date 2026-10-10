namespace Catalog.Tests;

using System.ComponentModel.DataAnnotations;
using Catalog.Api.Contracts;

/// <summary>
/// Unit tests verifying data annotation validation rules on CreateProductRequest DTO.
/// 
/// WHY WE CREATED THIS:
/// In microservices, input validation at the API boundary is the first line of defense.
/// These tests verify that malformed payloads (e.g. empty names, negative prices, or oversized text)
/// are rejected before hitting controllers, services, or the database.
/// 
/// CONCEPT: MODEL VALIDATION TESTING
/// In ASP.NET Core, the framework automatically executes System.ComponentModel.DataAnnotations.Validator
/// when an endpoint receives [FromBody]. By using Validator.TryValidateObject directly in our unit tests,
/// we test the exact validation rules in complete isolation without needing to spin up a web server.
/// </summary>
public class CreateProductRequestValidationTests
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
        var request = new CreateProductRequest(
            Name: "Mechanical Keyboard",
            Description: "RGB backlit mechanical keyboard with hot-swappable switches",
            Price: 89.99m);

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyOrNullName_ShouldFailValidation(string? invalidName)
    {
        // Arrange
        var request = new CreateProductRequest(
            Name: invalidName!,
            Description: "A valid description",
            Price: 49.99m);

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateProductRequest.Name)));
    }

    [Fact]
    public void NameExceeding200Characters_ShouldFailValidation()
    {
        // Arrange
        var oversizedName = new string('A', 201);
        var request = new CreateProductRequest(
            Name: oversizedName,
            Description: "A valid description",
            Price: 49.99m);

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateProductRequest.Name)));
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(-1.00)]
    [InlineData(-99.99)]
    public void ZeroOrNegativePrice_ShouldFailValidation(decimal invalidPrice)
    {
        // Arrange
        var request = new CreateProductRequest(
            Name: "Gaming Mouse",
            Description: "High DPI optical sensor",
            Price: invalidPrice);

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateProductRequest.Price)));
    }

    [Fact]
    public void DescriptionExceeding1000Characters_ShouldFailValidation()
    {
        // Arrange
        var oversizedDescription = new string('D', 1001);
        var request = new CreateProductRequest(
            Name: "USB-C Hub",
            Description: oversizedDescription,
            Price: 29.99m);

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateProductRequest.Description)));
    }
}
