using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Store.Shared.Configuration;

namespace Store.Shared.Authentication;

/// <summary>
/// Authenticates service-to-service requests carrying the shared key in the
/// <see cref="InternalApiOptions.HeaderName"/> header. Requests without the header are left to
/// other schemes; requests with a wrong key fail and are challenged with 401.
/// </summary>
public sealed class InternalApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptions<InternalApiOptions> _internalApiOptions;

    public InternalApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<InternalApiOptions> internalApiOptions)
        : base(options, logger, encoder)
    {
        _internalApiOptions = internalApiOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(InternalApiOptions.HeaderName, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var presented = values.ToString();
        var expected = _internalApiOptions.Value.ApiKey;

        // Constant-time comparison so response timing does not leak how much of the key matched
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(presented),
            Encoding.UTF8.GetBytes(expected));

        if (!matches)
        {
            Logger.LogWarning("Rejected request to {Path} with an invalid internal API key", Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key"));
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, InternalApiKeyDefaults.ServiceIdentityName) },
            Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
