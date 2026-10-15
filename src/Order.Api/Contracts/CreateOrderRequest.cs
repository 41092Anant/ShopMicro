namespace Order.Api.Contracts;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Public request DTO for creating a new customer order.
/// 
/// CONCEPT: FAIL-FAST INPUT VALIDATION
/// Validating input at the API boundary ensures invalid requests fail immediately
/// before any downstream HTTP calls to Catalog.Api or database queries occur.
/// </summary>
public class CreateOrderRequest
{
    [Required(ErrorMessage = "Customer email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one order item is required.")]
    [MinLength(1, ErrorMessage = "An order must contain at least one item.")]
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

/// <summary>
/// Public request line item representing a product and desired quantity.
/// </summary>
public class CreateOrderItemRequest
{
    [Required(ErrorMessage = "ProductId is required.")]
    public Guid ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1,000.")]
    public int Quantity { get; set; }
}
