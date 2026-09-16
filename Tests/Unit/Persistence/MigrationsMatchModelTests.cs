using Microsoft.EntityFrameworkCore;
using Store.AuditLogService.Data;
using Store.CartService.Data;
using Store.IdentityService.Data;
using Store.OrderService.Data;
using Store.ProductService.Data;
using Xunit;

namespace Store.Tests.Unit.Persistence;

/// <summary>
/// A changed entity without a migration used to be invisible (OrderService even silenced the
/// warning). Every context is compared with its model snapshot here; the check needs the
/// Npgsql provider for type mapping but never opens a connection.
/// </summary>
public class MigrationsMatchModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused;Username=unused;Password=unused";

    private static DbContextOptions<TContext> Options<TContext>() where TContext : DbContext
        => new DbContextOptionsBuilder<TContext>().UseNpgsql(UnusedConnectionString).Options;

    [Fact]
    public void Identity_model_has_no_pending_changes()
    {
        using var context = new IdentityDbContext(Options<IdentityDbContext>());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Catalogue_model_has_no_pending_changes()
    {
        using var context = new ProductDbContext(Options<ProductDbContext>());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Cart_model_has_no_pending_changes()
    {
        using var context = new CartDbContext(Options<CartDbContext>());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Order_model_has_no_pending_changes()
    {
        using var context = new OrderDbContext(Options<OrderDbContext>());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Audit_model_has_no_pending_changes()
    {
        using var context = new AuditLogDbContext(Options<AuditLogDbContext>());
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
