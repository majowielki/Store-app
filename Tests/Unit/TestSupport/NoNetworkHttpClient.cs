using System.Net;

namespace Store.Tests.Unit.TestSupport;

/// <summary>
/// HttpClient for services under unit test that must never reach the network: every request
/// gets 503 Service Unavailable, so a test that accidentally depends on another service fails
/// deterministically instead of timing out or hitting localhost.
/// </summary>
public static class NoNetworkHttpClient
{
    public static HttpClient Create() => new(new NoNetworkHandler()) { BaseAddress = new Uri("http://no-network.test") };

    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "unit tests do not talk to other services",
                RequestMessage = request
            });
    }
}
