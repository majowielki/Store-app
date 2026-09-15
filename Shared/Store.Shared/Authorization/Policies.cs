using Store.Shared.Authentication;

namespace Store.Shared.Authorization;

/// <summary>
/// Authorization policy names registered by <see cref="AuthorizationExtensions.AddStoreAuthorization"/>.
/// Use these constants in <c>[Authorize(Policy = ...)]</c> and in YARP route configuration.
/// </summary>
public static class Policies
{
    /// <summary>Any signed-in store user: user, demo-admin or true-admin.</summary>
    public const string User = "User";

    /// <summary>Read access to admin views: true-admin and demo-admin.</summary>
    public const string Admin = "Admin";

    /// <summary>Changes to catalogue, users and orders: true-admin only - demo-admin is read-only.</summary>
    public const string AdminWrite = "AdminWrite";

    /// <summary>Trusted service-to-service caller presenting the internal API key.</summary>
    public const string InternalService = InternalApiKeyDefaults.PolicyName;
}
