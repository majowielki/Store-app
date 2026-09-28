using Store.BuildingBlocks.Authorization;
using System.Security.Claims;

namespace Store.ReviewService.Services;

/// <summary>
/// Who is looking at the reviews, as far as they matter here: a signed-in customer sees their own
/// reviews in any state, and on a shared demo account "their own" means the ones written in this
/// sign-in session - the other visitors of the same account never see them.
/// </summary>
public sealed record ReviewViewer(string? UserId, Guid? DemoSessionId, bool IsDemoAccount, bool IsDemoAdmin, string AuthorName)
{
    public static readonly ReviewViewer Anonymous = new(null, null, false, false, string.Empty);

    public bool SignedIn => UserId is not null;

    public static ReviewViewer From(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Anonymous;
        }

        var isDemo = principal.IsDemoAccount();
        return new ReviewViewer(
            userId,
            isDemo ? principal.GetSessionId() : null,
            isDemo,
            principal.IsDemoAdmin(),
            AuthorNameOf(principal.FindFirst("firstName")?.Value, principal.FindFirst("lastName")?.Value, principal.FindFirst("displayName")?.Value));
    }

    /// <summary>"Anna N." - a first name and the initial of the last one, never the full name or the e-mail.</summary>
    public static string AuthorNameOf(string? firstName, string? lastName, string? displayName)
    {
        var first = firstName?.Trim();
        var last = lastName?.Trim();
        var name = !string.IsNullOrEmpty(first)
            ? string.IsNullOrEmpty(last) ? first : $"{first} {char.ToUpperInvariant(last[0])}."
            : displayName?.Trim();

        if (string.IsNullOrEmpty(name) || name.Contains('@'))
        {
            return "Customer";
        }

        return name.Length <= Models.ReviewConstraints.AuthorNameMaxLength ? name : name[..Models.ReviewConstraints.AuthorNameMaxLength];
    }
}
