namespace Store.IdentityService;

/// <summary>
/// The one definition of what a password must look like, used by the request validator
/// (so the client hears about it before Identity does) and by the Identity options (so
/// nothing else can create a weaker one).
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 100;
    public const bool RequireDigit = true;
    public const bool RequireLowercase = true;
    public const bool RequireUppercase = true;
    public const bool RequireNonAlphanumeric = true;
}
