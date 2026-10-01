using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Store.BuildingBlocks.Persistence;

/// <summary>Transaction boundaries shared by repositories, including an existing consumer inbox transaction.</summary>
public static class StoreTransactions
{
    public static Task<IDbContextTransaction?> BeginStoreTransactionAsync(this DbContext context, CancellationToken cancellationToken = default)
        => context.Database.IsRelational() && context.Database.CurrentTransaction is null
            ? BeginAsync(context, cancellationToken)
            : Task.FromResult<IDbContextTransaction?>(null);

    private static async Task<IDbContextTransaction?> BeginAsync(DbContext context, CancellationToken cancellationToken)
        => await context.Database.BeginTransactionAsync(cancellationToken);

    /// <summary>Serializes writers for a logical key, including keys without an existing row. Requires a transaction.</summary>
    public static Task LockKeyAsync(this DbContext context, string key, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational()) return Task.CompletedTask;
        if (context.Database.CurrentTransaction is null) throw new InvalidOperationException("A transaction is required before acquiring an advisory lock.");
        return context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
    }
}
