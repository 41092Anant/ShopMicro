namespace Catalog.Tests;

using System.ComponentModel.DataAnnotations;
using Catalog.Api.Contracts;

/// <summary>
/// Unit tests verifying data annotation validation rules on CreateCategoryRequest DTO.
/// </summary>
public class CreateCategoryRequestValidationTests
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
        var request = new CreateCategoryRequest("Electronics", "Consumer electronic products and accessories");

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
        var request = new CreateCategoryRequest(invalidName!, "Valid description");

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCategoryRequest.Name)));
    }

    [Fact]
    public void NameExceeding100Characters_ShouldFailValidation()
    {
        // Arrange
        var oversizedName = new string('C', 101);
        var request = new CreateCategoryRequest(oversizedName, "Valid description");

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCategoryRequest.Name)));
    }

    [Fact]
    public void DescriptionExceeding500Characters_ShouldFailValidation()
    {
        // Arrange
        var oversizedDescription = new string('D', 501);
        var request = new CreateCategoryRequest("Office Supplies", oversizedDescription);

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCategoryRequest.Description)));
    }
}
