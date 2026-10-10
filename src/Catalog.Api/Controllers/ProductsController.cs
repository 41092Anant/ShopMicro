namespace Catalog.Api.Controllers;

using Catalog.Api.Contracts;
using Catalog.Api.Services;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// HTTP Controller exposing product operations for the Catalog microservice.
/// 
/// WHY WE CREATED THIS:
/// Controllers are responsible for HTTP-level concerns:
/// - Parsing HTTP request URLs, headers, and query parameters.
/// - Validating request payloads.
/// - Delegating business logic to IProductService.
/// - Returning appropriate HTTP status codes (200 OK, 201 Created, 400 Bad Request, 404 Not Found).
/// 
/// CONCEPT: [ApiController] ATTRIBUTE
/// In ASP.NET Core, adding [ApiController] enables powerful automated features:
/// 1. Automatic model validation: If request validation attributes fail, it automatically returns
///    a 400 Bad Request response with RFC 7807 problem details without manual 'if (!ModelState.IsValid)' checks.
/// 2. Automatic source binding inference: Infers where parameters come from ([FromBody], [FromRoute], etc.).
/// 
/// CONCEPT: [Route("products")]
/// Establishes the URL path for this controller: http://localhost:5001/products
/// </summary>
[ApiController]
[Route("products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// GET /products - Returns a list of all products in the catalog.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// GET /products/{id} - Returns a single product by its unique GUID identifier.
    /// 
    /// CONCEPT: ROUTE CONSTRAINT "{id:guid}"
    /// Restricts the route parameter to valid GUID format. If a caller requests /products/123-abc (not a guid),
    /// ASP.NET Core automatically rejects it with 404 before reaching our code.
    /// 
    /// CONCEPT: RFC 7807 ProblemDetails
    /// If product is missing, we return a structured ProblemDetails object with 404 Not Found status.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Product Not Found",
                Detail = $"Product with ID '{id}' was not found."
            });
        }

        return Ok(product);
    }

    /// <summary>
    /// POST /products - Creates a new product in the catalog.
    /// 
    /// CONCEPT: HTTP 201 CreatedAtAction
    /// In REST API standards, when a new resource is created:
    /// 1. Return HTTP 201 Created (not just 200 OK).
    /// 2. Include a 'Location' HTTP header containing the URL to read the newly created resource (/products/{id}).
    /// 3. Return the created product body.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
