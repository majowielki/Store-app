using Store.IdentityService.Data;
using Store.Tests.Integration.TestSupport;

namespace Store.Tests.Integration.Identity;

public sealed class IdentityApiFactory : StoreApiFactory<IdentityDbContext>
{
    public IdentityApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => "store_identity_test";
}
