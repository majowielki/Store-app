using Store.ContentService.Data;
using Store.Tests.Integration.TestSupport;

namespace Store.Tests.Integration.Content;

public sealed class ContentApiFactory : StoreApiFactory<ContentDbContext>
{
    public ContentApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => "store_content_test";
}
