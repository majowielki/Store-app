using Microsoft.AspNetCore.Identity;

namespace Store.IdentityService.Models;

public class ApplicationUser : IdentityUser
{
    // Column lengths, shared with the request validators
    public const int NameMaxLength = 100;
    public const int AddressMaxLength = 300;
    public const int EmailMaxLength = 256; // what Identity gives the e-mail columns

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    /// <summary>One free-text delivery address; the checkout can store it from the order.</summary>
    public string? SimpleAddress { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public string DisplayName => !string.IsNullOrEmpty($"{FirstName} {LastName}".Trim())
        ? $"{FirstName} {LastName}".Trim()
        : UserName ?? Email ?? "Unknown User";
}
