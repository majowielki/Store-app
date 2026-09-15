using Microsoft.Extensions.Options;
using Store.Shared.Configuration;

namespace Store.Shared.Authentication;

/// <summary>
/// Adds the shared internal API key to every outgoing request of the HttpClient it is
/// attached to, so typed clients calling internal endpoints of other services authenticate.
/// </summary>
public sealed class InternalApiKeyMessageHandler : DelegatingHandler
{
    private readonly IOptions<InternalApiOptions> _options;

    public InternalApiKeyMessageHandler(IOptions<InternalApiOptions> options)
    {
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(InternalApiOptions.HeaderName);
        request.Headers.Add(InternalApiOptions.HeaderName, _options.Value.ApiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
