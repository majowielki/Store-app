using Store.ProductService.Data;
using Store.Tests.Integration.TestSupport;

namespace Store.Tests.Integration.Catalog;

public sealed class CatalogApiFactory : StoreApiFactory<ProductDbContext>
{
    public CatalogApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => "store_product_test";
}
