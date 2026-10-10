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
}
