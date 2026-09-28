namespace Store.Contracts.Authorization;

/// <summary>User ids as the identity service issues them (the "sub" of a token) and every service stores them.</summary>
public static class UserIds
{
    /// <summary>The longest id: ASP.NET Identity's key length, the column length everywhere a user id is kept.</summary>
    public const int MaxLength = 450;
}
