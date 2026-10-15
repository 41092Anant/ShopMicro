namespace Order.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Order.Api.Contracts;
using Order.Api.Services;

/// <summary>
/// HTTP Controller exposing order operations for the Order microservice.
/// 
/// CONCEPT: LEAN CONTROLLERS & RFC 7807 ERROR MAPPING
/// Controllers are responsible for HTTP-level concerns:
/// - Parsing request parameters and routing.
/// - Model validation via [ApiController].
/// - Mapping business outcomes or exceptions to proper HTTP status codes.
/// - Emitting standardized RFC 7807 ProblemDetails for all error conditions.
/// </summary>
[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// GET /orders/{id} - Returns an order by its unique GUID identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);

        if (order is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Order Not Found",
                Detail = $"Order with ID '{id}' was not found."
            });
        }

        return Ok(order);
    }

    /// <summary>
    /// POST /orders - Creates a new customer order after validating line items against Catalog.Api.
    /// 
    /// CONCEPT: INTER-SERVICE ERROR RESILIENCE
    /// If Catalog.Api is temporarily down or unreachable, an HttpRequestException is caught
    /// and converted into an RFC 7807 HTTP 503 Service Unavailable response, informing the client cleanly.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _orderService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Order Request",
                Detail = ex.Message
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Downstream Catalog.Api communication failure during order placement.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Catalog Service Unavailable",
                Detail = "Unable to validate products with the Catalog service. Please retry shortly."
            });
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Downstream Catalog.Api request timed out during order placement.");
            return StatusCode(StatusCodes.Status504GatewayTimeout, new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "Catalog Service Timeout",
                Detail = "The Catalog service timed out while validating products. Please retry shortly."
            });
        }
    }
}
