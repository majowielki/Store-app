using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Store.Shared.Models;
using System.Net;

namespace Store.Shared.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger _logger;

    protected BaseApiController(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    /// <typeparam name="T">Type of data</typeparam>
    /// <param name="data">Response data</param>
    /// <param name="message">Success message</param>
    /// <returns>Standardized API response</returns>
    protected IActionResult SuccessResponse<T>(T data, string message = "Success")
    {
        var response = ApiResponse<T>.Success(data, message);
        return Ok(response);
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="errors">Detailed error information</param>
    /// <returns>Standardized error response</returns>
    protected IActionResult ErrorResponse(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null)
    {
        var errorList = errors?.SelectMany(kvp => kvp.Value).ToList();
        var response = ApiResponse<object>.Error(message, (HttpStatusCode)statusCode, errorList);
        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Creates a standardized validation error response
    /// </summary>
    /// <param name="modelState">Model state with validation errors</param>
    /// <returns>Standardized validation error response</returns>
    protected IActionResult ValidationErrorResponse(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        return ErrorResponse("Validation failed", 400, errors);
    }

    /// <summary>
    /// Creates a not found response
    /// </summary>
    /// <param name="message">Not found message</param>
    /// <returns>Standardized not found response</returns>
    protected IActionResult NotFoundResponse(string message = "Resource not found")
    {
        return ErrorResponse(message, 404);
    }

    /// <summary>
    /// Gets the current user ID from the JWT token
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    protected string? GetCurrentUserId()
    {
        return User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Gets the current user email from the JWT token
    /// </summary>
    /// <returns>User email or null if not authenticated</returns>
    protected string? GetCurrentUserEmail()
    {
        return User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Handles exceptions with consistent error responses
    /// </summary>
    /// <param name="ex">Exception to handle</param>
    /// <param name="customMessage">Custom error message</param>
    /// <returns>Standardized error response</returns>
    protected IActionResult HandleException(Exception ex, string customMessage = "An error occurred")
    {
        _logger.LogError(ex, "API Error: {Message}", customMessage);

        // Don't expose internal error details in production
        var message = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
            ? ex.Message 
            : customMessage;

        return ErrorResponse(message, 500);
    }
}
