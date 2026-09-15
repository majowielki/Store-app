namespace Store.Shared.Authentication;

/// <summary>
/// Names used by the internal API key authentication scheme
/// </summary>
public static class InternalApiKeyDefaults
{
    /// <summary>Authentication scheme name</summary>
    public const string AuthenticationScheme = "InternalApiKey";

    /// <summary>Authorization policy that only trusted services satisfy</summary>
    public const string PolicyName = "InternalService";

    /// <summary>Identity name assigned to an authenticated service caller</summary>
    public const string ServiceIdentityName = "internal-service";
}
