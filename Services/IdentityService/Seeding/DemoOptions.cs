using System.ComponentModel.DataAnnotations;

namespace Store.IdentityService.Seeding;

/// <summary>
/// The demo accounts (<c>Demo</c> section). Off by default: a deployment that wants the
/// showcase accounts and the password-less demo logins turns them on explicitly. The
/// passwords are optional - the demo endpoints sign the accounts in without one, so an
/// account seeded without a configured password gets a random one nobody knows.
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public bool Enabled { get; init; }

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string UserEmail { get; init; } = "demo@store.com";

    /// <summary>Password of the demo customer, when signing in with one should work at all.</summary>
    public string? UserPassword { get; init; }

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string AdminEmail { get; init; } = "demo-admin@store.com";

    /// <summary>Password of the demo administrator, when signing in with one should work at all.</summary>
    public string? AdminPassword { get; init; }
}
