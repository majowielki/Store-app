using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Store.Shared.Authentication;
using Store.Shared.Configuration;
using System.Net;
using System.Text.Encodings.Web;
using Xunit;

namespace Store.Tests.Unit.Shared;

/// <summary>
/// Regression: service-to-service calls must carry the shared key; everything else is rejected.
/// </summary>
public class InternalApiKeyAuthenticationTests
{
    private const string ValidKey = "0123456789abcdef0123456789abcdef-internal-test-key";

    private static async Task<InternalApiKeyAuthenticationHandler> CreateHandler(string? presentedKey)
    {
        var schemeOptions = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        schemeOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

        var handler = new InternalApiKeyAuthenticationHandler(
            schemeOptions.Object,
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            Options.Create(new InternalApiOptions { ApiKey = ValidKey }));

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auditlog/internal";
        if (presentedKey is not null)
        {
            context.Request.Headers[InternalApiOptions.HeaderName] = presentedKey;
        }

        var scheme = new AuthenticationScheme(
            InternalApiKeyDefaults.AuthenticationScheme,
            displayName: null,
            typeof(InternalApiKeyAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);
        return handler;
    }

    [Fact]
    public async Task Request_without_header_is_not_authenticated_by_this_scheme()
    {
        var handler = await CreateHandler(presentedKey: null);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.None);
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("wrong-key")]
    [InlineData("0123456789abcdef0123456789abcdef-internal-test-keY")]
    public async Task Request_with_wrong_key_fails(string presentedKey)
    {
        var handler = await CreateHandler(presentedKey);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task Request_with_correct_key_is_authenticated_as_internal_service()
    {
        var handler = await CreateHandler(ValidKey);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(InternalApiKeyDefaults.AuthenticationScheme, result.Ticket!.AuthenticationScheme);
        Assert.Equal(InternalApiKeyDefaults.ServiceIdentityName, result.Principal!.Identity!.Name);
        Assert.True(result.Principal.Identity.IsAuthenticated);
    }

    [Fact]
    public async Task Message_handler_adds_the_key_to_outgoing_requests()
    {
        HttpRequestMessage? captured = null;
        var inner = new Mock<HttpMessageHandler>();
        inner.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var client = new HttpClient(new InternalApiKeyMessageHandler(Options.Create(new InternalApiOptions { ApiKey = ValidKey }))
        {
            InnerHandler = inner.Object
        })
        {
            BaseAddress = new Uri("http://auditlogservice")
        };

        await client.PostAsync("/api/auditlog/internal", new StringContent("{}"));

        Assert.NotNull(captured);
        Assert.Equal(ValidKey, captured!.Headers.GetValues(InternalApiOptions.HeaderName).Single());
    }
}
