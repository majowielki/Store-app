using Microsoft.AspNetCore.Http;

namespace Store.BuildingBlocks.Api;

/// <summary>
/// An error the caller can act on, thrown by application code and turned into an
/// RFC 9457 problem response by <see cref="StoreExceptionHandler"/>. Services throw the
/// derived types; controllers do not translate outcomes into status codes themselves.
/// </summary>
public abstract class ApiException : Exception
{
    protected ApiException(int statusCode, string message, string? title = null)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    /// <summary>HTTP status the response carries.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// Short summary of the problem type; null takes the reason phrase of the status code.
    /// </summary>
    public string? Title { get; }

    /// <summary>Field-level messages for validation problems; null for the other types.</summary>
    public virtual IDictionary<string, string[]>? Errors => null;
}

/// <summary>The resource the request addresses does not exist (or is not visible to the caller).</summary>
public class NotFoundException : ApiException
{
    public NotFoundException(string message)
        : base(StatusCodes.Status404NotFound, message)
    {
    }

    /// <summary>"Order 42 was not found" - the resource type and its key.</summary>
    public NotFoundException(string resource, object key)
        : this($"{resource} {key} was not found")
    {
    }
}

/// <summary>The caller is signed in but may not access this resource.</summary>
public class ForbiddenException : ApiException
{
    public ForbiddenException(string message)
        : base(StatusCodes.Status403Forbidden, message)
    {
    }
}

/// <summary>The request cannot be applied to the resource in its current state.</summary>
public class ConflictException : ApiException
{
    public ConflictException(string message)
        : base(StatusCodes.Status409Conflict, message)
    {
    }
}

/// <summary>
/// The request is well formed but a business rule rejects its content - an empty cart at
/// checkout, a product that does not exist in a cart line. Model validation (shapes, ranges,
/// required fields) answers with the same status through the validation pipeline.
/// </summary>
public class DomainValidationException : ApiException
{
    private readonly IDictionary<string, string[]>? _errors;

    public DomainValidationException(string message)
        : base(StatusCodes.Status422UnprocessableEntity, message)
    {
    }

    /// <param name="message">Summary of what was rejected</param>
    /// <param name="errors">Messages per field, the way validation problems report them</param>
    public DomainValidationException(string message, IDictionary<string, string[]> errors)
        : this(message)
    {
        _errors = errors;
    }

    public override IDictionary<string, string[]>? Errors => _errors;
}
