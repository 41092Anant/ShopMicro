namespace Catalog.Api.Controllers;

using Catalog.Api.Contracts;
using Catalog.Api.Services;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// HTTP Controller exposing product category operations for the Catalog microservice.
/// </summary>
[ApiController]
[Route("categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// GET /categories - Retrieves all product categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// GET /categories/{id} - Retrieves a category by its GUID identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Category Not Found",
                Detail = $"Category with ID '{id}' was not found."
            });
        }

        return Ok(category);
    }

    /// <summary>
    /// POST /categories - Creates a new category.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// GET /categories/{id}/products - Retrieves all products belonging to a specific category.
    /// </summary>
    [HttpGet("{id:guid}/products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetProductsByCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var products = await _categoryService.GetProductsByCategoryIdAsync(id, cancellationToken);

        if (products is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Category Not Found",
                Detail = $"Category with ID '{id}' was not found."
            });
        }

        return Ok(products);
    }
}
