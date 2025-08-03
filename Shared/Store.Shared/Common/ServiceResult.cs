namespace Store.Shared.Common;

/// <summary>
/// Represents the result of a service operation
/// </summary>
/// <typeparam name="T">Type of the result data</typeparam>
public class ServiceResult<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Result data (only available when IsSuccess is true)
    /// </summary>
    public T? Data { get; private set; }

    /// <summary>
    /// Error message (only available when IsSuccess is false)
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Error code for categorizing errors
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public IDictionary<string, string[]>? ValidationErrors { get; private set; }

    private ServiceResult(bool isSuccess, T? data, string? errorMessage, string? errorCode, IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <param name="data">Result data</param>
    /// <returns>Successful service result</returns>
    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>(true, data, null, null);
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorCode">Error code</param>
    /// <returns>Failed service result</returns>
    public static ServiceResult<T> Failure(string errorMessage, string? errorCode = null)
    {
        return new ServiceResult<T>(false, default, errorMessage, errorCode);
    }

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="validationErrors">Validation errors</param>
    /// <param name="errorCode">Error code</param>
    /// <returns>Failed service result with validation errors</returns>
    public static ServiceResult<T> ValidationFailure(string errorMessage, IDictionary<string, string[]> validationErrors, string? errorCode = "VALIDATION_ERROR")
    {
        return new ServiceResult<T>(false, default, errorMessage, errorCode, validationErrors);
    }

    /// <summary>
    /// Creates a not found result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Not found service result</returns>
    public static ServiceResult<T> NotFound(string errorMessage = "Resource not found")
    {
        return new ServiceResult<T>(false, default, errorMessage, "NOT_FOUND");
    }

    /// <summary>
    /// Creates an unauthorized result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Unauthorized service result</returns>
    public static ServiceResult<T> Unauthorized(string errorMessage = "Unauthorized access")
    {
        return new ServiceResult<T>(false, default, errorMessage, "UNAUTHORIZED");
    }
}

/// <summary>
/// Non-generic service result for operations that don't return data
/// </summary>
public class ServiceResult
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Error message (only available when IsSuccess is false)
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Error code for categorizing errors
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public IDictionary<string, string[]>? ValidationErrors { get; private set; }

    private ServiceResult(bool isSuccess, string? errorMessage, string? errorCode, IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>Successful service result</returns>
    public static ServiceResult Success()
    {
        return new ServiceResult(true, null, null);
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorCode">Error code</param>
    /// <returns>Failed service result</returns>
    public static ServiceResult Failure(string errorMessage, string? errorCode = null)
    {
        return new ServiceResult(false, errorMessage, errorCode);
    }

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="validationErrors">Validation errors</param>
    /// <param name="errorCode">Error code</param>
    /// <returns>Failed service result with validation errors</returns>
    public static ServiceResult ValidationFailure(string errorMessage, IDictionary<string, string[]> validationErrors, string? errorCode = "VALIDATION_ERROR")
    {
        return new ServiceResult(false, errorMessage, errorCode, validationErrors);
    }

    /// <summary>
    /// Creates a not found result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Not found service result</returns>
    public static ServiceResult NotFound(string errorMessage = "Resource not found")
    {
        return new ServiceResult(false, errorMessage, "NOT_FOUND");
    }

    /// <summary>
    /// Creates an unauthorized result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Unauthorized service result</returns>
    public static ServiceResult Unauthorized(string errorMessage = "Unauthorized access")
    {
        return new ServiceResult(false, errorMessage, "UNAUTHORIZED");
    }
}
