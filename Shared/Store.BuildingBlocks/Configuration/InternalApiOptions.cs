using System.ComponentModel.DataAnnotations;

namespace Store.BuildingBlocks.Configuration;

/// <summary>
/// Shared secret used for service-to-service calls that must not be reachable by end users
/// (the product and cart snapshot endpoints). Bound from the <c>InternalApi</c> section and
/// validated on start. Interim solution until services get their own credentials.
/// </summary>
public sealed class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    /// <summary>Name of the request header carrying the key.</summary>
    public const string HeaderName = "X-Internal-Api-Key";

    /// <summary>
    /// Random key shared by the callers and the receiving service. Never commit it: use
    /// <c>dotnet user-secrets</c> locally and the <c>InternalApi__ApiKey</c> environment
    /// variable in containers.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string ApiKey { get; init; } = string.Empty;
}
