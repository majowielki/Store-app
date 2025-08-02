using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Store.IdentityService.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    [StringLength(300)]
    public string? SimpleAddress { get; set; } // Simplified single address field
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    // Computed properties
    public string DisplayName => !string.IsNullOrEmpty($"{FirstName} {LastName}".Trim()) 
        ? $"{FirstName} {LastName}".Trim() 
        : UserName ?? Email ?? "Unknown User";
}
