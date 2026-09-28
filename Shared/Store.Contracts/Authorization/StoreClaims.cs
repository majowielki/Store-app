namespace Store.Contracts.Authorization;

/// <summary>
/// The store's own claims in an access token, besides the standard ones (user id, name, e-mail).
/// Their names are not in the JWT handler's mapping table, so they arrive as written - except
/// <see cref="Role"/>, which the bearer handlers map to <c>ClaimTypes.Role</c>.
/// </summary>
public static class StoreClaims
{
    /// <summary>One claim per role (<see cref="Roles"/>).</summary>
    public const string Role = "role";

    public const string FirstName = "firstName";

    public const string LastName = "lastName";

    /// <summary>The name the shop greets the user by.</summary>
    public const string DisplayName = "displayName";

    /// <summary>The sign-in session (the refresh token family); the same across refreshes, new at each sign-in.</summary>
    public const string SessionId = "session_id";

    /// <summary><see cref="DemoAccountValue"/> for the showcase accounts every visitor shares (the demo user and the demo administrator).</summary>
    public const string DemoAccount = "demo_account";

    /// <summary>The value of <see cref="DemoAccount"/>; the claim is absent on every other account.</summary>
    public const string DemoAccountValue = "true";
}
