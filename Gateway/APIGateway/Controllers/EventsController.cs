using Microsoft.AspNetCore.Mvc;
using Store.Shared.MessageBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace Store.GatewayService.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiPolicy")]
[Authorize(Policy = "UserOrAdmin")]
public class EventsController : ControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IMessageBus messageBus, ILogger<EventsController> logger)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Test endpoint for publishing user registered events
    /// </summary>
    /// <param name="request">User registration event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event publication result</returns>
    [HttpPost("test-user-registered")]
    [ProducesResponseType(typeof(EventPublishedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestUserRegisteredEvent(
        [FromBody] TestUserRegisteredRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for TestUserRegisteredEvent: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userRegisteredEvent = new UserRegisteredEvent
            {
                UserId = request.UserId,
                Email = request.Email,
                UserName = request.UserName,
                RegisteredAt = DateTime.UtcNow
            };

            await _messageBus.PublishAsync(userRegisteredEvent, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully published UserRegisteredEvent for user: {UserId}, Email: {Email}", 
                request.UserId, request.Email);
            
            return Ok(new EventPublishedResponse
            {
                Message = "User registered event published successfully",
                EventId = userRegisteredEvent.Id,
                EventType = nameof(UserRegisteredEvent),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing UserRegisteredEvent for user: {UserId}", request.UserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while publishing the event" });
        }
    }

    /// <summary>
    /// Test endpoint for publishing order created events
    /// </summary>
    /// <param name="request">Order creation event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event publication result</returns>
    [HttpPost("test-order-created")]
    [ProducesResponseType(typeof(EventPublishedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestOrderCreatedEvent(
        [FromBody] TestOrderCreatedRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for TestOrderCreatedEvent: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = request.OrderId,
                UserId = request.UserId,
                TotalAmount = request.TotalAmount,
                Items = request.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            await _messageBus.PublishAsync(orderCreatedEvent, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully published OrderCreatedEvent for order: {OrderId}, Total: {TotalAmount}", 
                request.OrderId, request.TotalAmount);
            
            return Ok(new EventPublishedResponse
            {
                Message = "Order created event published successfully",
                EventId = orderCreatedEvent.Id,
                EventType = nameof(OrderCreatedEvent),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrderCreatedEvent for order: {OrderId}", request.OrderId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while publishing the event" });
        }
    }

    /// <summary>
    /// Test endpoint for publishing product inventory updated events
    /// </summary>
    /// <param name="request">Product inventory update event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event publication result</returns>
    [HttpPost("test-product-inventory")]
    [ProducesResponseType(typeof(EventPublishedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestProductInventoryEvent(
        [FromBody] TestProductInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for TestProductInventoryEvent: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var inventoryEvent = new ProductInventoryUpdatedEvent
            {
                ProductId = request.ProductId,
                NewStock = request.NewStock,
                PreviousStock = request.PreviousStock
            };

            await _messageBus.PublishAsync(inventoryEvent, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully published ProductInventoryUpdatedEvent for product: {ProductId}, NewStock: {NewStock}", 
                request.ProductId, request.NewStock);
            
            return Ok(new EventPublishedResponse
            {
                Message = "Product inventory updated event published successfully",
                EventId = inventoryEvent.Id,
                EventType = nameof(ProductInventoryUpdatedEvent),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing ProductInventoryUpdatedEvent for product: {ProductId}", request.ProductId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while publishing the event" });
        }
    }

    /// <summary>
    /// Health check endpoint for the events controller
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "EventsController", Timestamp = DateTime.UtcNow });
    }
}

// Request DTOs with validation
public class TestUserRegisteredRequest
{
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "UserName is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "UserName must be between 2 and 100 characters")]
    public string UserName { get; set; } = string.Empty;
}

public class TestOrderCreatedRequest
{
    [Required(ErrorMessage = "OrderId is required")]
    public Guid OrderId { get; set; }

    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "TotalAmount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "TotalAmount must be greater than 0")]
    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "Items are required")]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<TestOrderItem> Items { get; set; } = new();
}

public class TestOrderItem
{
    [Required(ErrorMessage = "ProductId is required")]
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
}

public class TestProductInventoryRequest
{
    [Required(ErrorMessage = "ProductId is required")]
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "NewStock is required")]
    [Range(0, int.MaxValue, ErrorMessage = "NewStock must be non-negative")]
    public int NewStock { get; set; }

    [Required(ErrorMessage = "PreviousStock is required")]
    [Range(0, int.MaxValue, ErrorMessage = "PreviousStock must be non-negative")]
    public int PreviousStock { get; set; }
}

// Response DTO
public class EventPublishedResponse
{
    public string Message { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}