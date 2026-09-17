using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;

namespace Store.BuildingBlocks.Api;

/// <summary>
/// Every error a service returns is an RFC 9457 problem (<c>application/problem+json</c>) with
/// the status code that describes it: 404 for a missing resource, 403 for a caller who may not
/// see it, 409 for a state conflict, 422 for a request whose content was rejected, 500 for a
/// fault. Success responses are plain DTOs.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Registers the problem-details writer, the handler that maps exceptions to problems and
    /// the validation responses (422). Pair with <see cref="UseStoreProblemDetails"/>.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddStoreProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path);
        services.AddExceptionHandler<StoreExceptionHandler>();

        // Model binding and data-annotation failures: the same shape and status as FluentValidation
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context => ValidationProblem(context.HttpContext, context.ModelState));

        return services;
    }

    /// <summary>
    /// Turns unhandled exceptions and empty error responses (401 from the token handler, 403
    /// from authorization, 404 for unknown routes) into problem responses. Register first in
    /// the pipeline.
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder UseStoreProblemDetails(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }

    /// <summary>A 422 validation problem built from the model state, through the registered factory.</summary>
    internal static IActionResult ValidationProblem(HttpContext httpContext, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
        var factory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(httpContext, modelState, StatusCodes.Status422UnprocessableEntity);
        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };
    }
}

/// <summary>FluentValidation failures answer with 422, like every other rejected request content.</summary>
public sealed class UnprocessableEntityResultFactory : IFluentValidationAutoValidationResultFactory
{
    public Task<IActionResult?> CreateActionResult(
        ActionExecutingContext context,
        ValidationProblemDetails? validationProblemDetails,
        IDictionary<IValidationContext, ValidationResult> validationResults)
        => Task.FromResult<IActionResult?>(ProblemDetailsExtensions.ValidationProblem(context.HttpContext, context.ModelState));
}

/// <summary>
/// Maps exceptions to problem responses. <see cref="ApiException"/> carries its own status;
/// anything else is a fault: logged with the stack trace and answered with 500 and a trace id,
/// never with the exception text outside Development.
/// </summary>
public sealed class StoreExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<StoreExceptionHandler> _logger;

    public StoreExceptionHandler(IProblemDetailsService problemDetails, IHostEnvironment environment, ILogger<StoreExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to answer
            _logger.LogDebug("Request {Method} {Path} was cancelled by the client", httpContext.Request.Method, httpContext.Request.Path);
            return true;
        }

        var (status, title, detail) = exception switch
        {
            ApiException api => (api.StatusCode, api.Title, api.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, null, "The request needs a valid signed-in user"),
            BadHttpRequestException bad => (bad.StatusCode, null, bad.Message),
            _ => (StatusCodes.Status500InternalServerError, null, _environment.IsDevelopment() ? exception.ToString() : null)
        };

        // Title and type of the well-known status codes are filled in by the writer
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        if (exception is ApiException { Errors: { } errors })
        {
            problem.Extensions["errors"] = errors;
        }

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogInformation("{Method} {Path} answered {Status}: {Detail}", httpContext.Request.Method, httpContext.Request.Path, status, detail);
        }

        // A client that does not accept JSON still gets the status code, with an empty body
        httpContext.Response.StatusCode = status;
        await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
        return true;
    }
}
