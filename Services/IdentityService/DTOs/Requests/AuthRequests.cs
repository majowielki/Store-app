using System.ComponentModel.DataAnnotations;

namespace Store.IdentityService.DTOs.Requests;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class DemoLoginRequest
{
    // No authentication required - just return demo user token
}

public class DemoAdminLoginRequest
{
    // No authentication required - just return demo admin token
}

public class RefreshTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}