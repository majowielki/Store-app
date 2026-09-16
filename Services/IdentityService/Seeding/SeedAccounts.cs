namespace Store.IdentityService.Seeding;

/// <summary>
/// Accounts IdentityService seeds at startup. Demo credentials are public by design (the demo
/// login endpoints hand them out); moving them behind a Demo:Enabled flag with values from
/// configuration is planned for the identity phase.
/// </summary>
public static class SeedAccounts
{
    public const string DemoUserEmail = "demo@store.com";
    public const string DemoUserPassword = "Demo123!";

    public const string DemoAdminEmail = "demo-admin@store.com";
    public const string DemoAdminPassword = "DemoAdmin123!";
}
