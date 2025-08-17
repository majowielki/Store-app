using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Store.GatewayService.Controllers;

[ApiController]
[Route("api/newsletter")]
public class NewsletterController : ControllerBase
{
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(ILogger<NewsletterController> logger)
    {
        _logger = logger;
    }

    [HttpPost("subscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Subscribe([FromBody] SubscribeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // TODO: integrate with real newsletter provider or database
        _logger.LogInformation("Newsletter subscription: {Email}", request.Email);
        return NoContent();
    }
}

public class SubscribeRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;
}
