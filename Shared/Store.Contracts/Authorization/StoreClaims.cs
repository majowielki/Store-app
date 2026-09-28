namespace Store.Contracts.Authorization;

/// <summary>
/// The store's own claims in an access token, besides the standard ones (user id, name, e-mail,
/// roles). Their names are not in the JWT handler's mapping table, so they arrive as written.
/// </summary>
public static class StoreClaims
{
    /// <summary>The sign-in session (the refresh token family); the same across refreshes, new at each sign-in.</summary>
    public const string SessionId = "session_id";

    /// <summary>"true" for the showcase accounts every visitor shares (the demo user and the demo administrator).</summary>
    public const string DemoAccount = "demo_account";
}
