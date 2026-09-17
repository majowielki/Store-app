namespace Store.IdentityService.DTOs.Requests;

/// <summary>Body of POST /api/v1/auth/register. Rules: <c>RegisterRequestValidator</c> and the password policy.</summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}

/// <summary>Body of POST /api/v1/auth/login. Rules: <c>LoginRequestValidator</c>.</summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
