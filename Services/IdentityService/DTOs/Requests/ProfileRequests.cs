namespace Store.IdentityService.DTOs.Requests;

/// <summary>Body of PUT /api/v1/auth/me/address; an empty value clears the address. Rules: <c>UpdateAddressRequestValidator</c>.</summary>
public class UpdateAddressRequest
{
    public string SimpleAddress { get; set; } = string.Empty;
}
