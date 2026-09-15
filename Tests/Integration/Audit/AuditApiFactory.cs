using Store.AuditLogService.Data;
using Store.Tests.Integration.TestSupport;

namespace Store.Tests.Integration.Audit;

public sealed class AuditApiFactory : StoreApiFactory<AuditLogDbContext>
{
    public AuditApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => "store_audit_test";
}
