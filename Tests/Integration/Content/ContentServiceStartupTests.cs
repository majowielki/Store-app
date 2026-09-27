using Store.Tests.Integration.TestSupport;
using System.Net;
using Xunit;

namespace Store.Tests.Integration.Content;

/// <summary>The content service starts like every other service: migrated database, ready for traffic.</summary>
[Collection(PostgresTests.Name)]
public sealed class ContentServiceStartupTests : IClassFixture<ContentApiFactory>
{
    private readonly ContentApiFactory _factory;

    public ContentServiceStartupTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task The_service_migrates_its_database_and_reports_ready()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }
}
