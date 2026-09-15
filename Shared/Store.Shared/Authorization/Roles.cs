namespace Store.Shared.Authorization;

/// <summary>
/// Role names issued by IdentityService in the "role" claim. Single source of truth for every
/// service and the gateway (BLK-01, BLK-02, MAJ-09).
/// </summary>
public static class Roles
{
    /// <summary>Full administrator: may read and change everything.</summary>
    public const string TrueAdmin = "true-admin";

    /// <summary>Demo administrator: sees the admin panel but may not change data.</summary>
    public const string DemoAdmin = "demo-admin";

    /// <summary>Regular customer.</summary>
    public const string User = "user";

    /// <summary>Roles that may open the admin panel.</summary>
    public static readonly string[] Admins = { TrueAdmin, DemoAdmin };

    /// <summary>Every role a signed-in store user can have.</summary>
    public static readonly string[] All = { User, TrueAdmin, DemoAdmin };
}
