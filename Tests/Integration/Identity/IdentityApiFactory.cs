using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Store.IdentityService.Data;
using Store.Tests.Integration.TestSupport;

namespace Store.Tests.Integration.Identity;

public sealed class IdentityApiFactory : StoreApiFactory<IdentityDbContext>
{
    public IdentityApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => "store_identity_test";

    /// <summary>The service's clock; a test moves it past the refresh token reuse window.</summary>
    public TestClock Clock { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(Clock);
    }
}
