using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Store.BuildingBlocks.Persistence;

/// <summary>
/// The SQL names of an entity's table and columns, read from the EF model and quoted for the
/// database, for the few statements LINQ cannot express (row locks, SKIP LOCKED). A renamed
/// property, table or schema follows into the SQL instead of breaking it at run time.
/// </summary>
/// <typeparam name="TEntity">An entity of the context</typeparam>
public sealed class EntitySql<TEntity> where TEntity : class
{
    private readonly IEntityType _entity;
    private readonly StoreObjectIdentifier _table;
    private readonly ISqlGenerationHelper _sql;

    internal EntitySql(DbContext context)
    {
        _entity = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not an entity of {context.GetType().Name}.");
        var table = _entity.GetTableName()
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not mapped to a table.");
        _table = StoreObjectIdentifier.Table(table, _entity.GetSchema());
        _sql = context.GetService<ISqlGenerationHelper>();
        Table = _sql.DelimitIdentifier(table, _entity.GetSchema());
    }

    /// <summary>The table, quoted and qualified by its schema when it has one.</summary>
    public string Table { get; }

    /// <summary>The quoted column of <paramref name="property"/> (<c>e =&gt; e.Id</c>).</summary>
    public string Column<TValue>(Expression<Func<TEntity, TValue>> property)
    {
        var body = property.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : property.Body;
        var name = (body as MemberExpression)?.Member.Name
            ?? throw new ArgumentException("Expected a property of the entity, such as e => e.Id.", nameof(property));
        var column = _entity.FindProperty(name)?.GetColumnName(_table)
            ?? throw new ArgumentException($"{typeof(TEntity).Name}.{name} is not mapped to a column of {Table}.", nameof(property));
        return _sql.DelimitIdentifier(column);
    }
}

/// <summary>Raw SQL built from the model: <see cref="EntitySql{TEntity}"/> and the row locks made with it.</summary>
public static class EntitySqlExtensions
{
    /// <summary>The table and column names of <typeparamref name="TEntity"/> in <paramref name="context"/>.</summary>
    public static EntitySql<TEntity> Sql<TEntity>(this DbContext context) where TEntity : class => new(context);

    /// <summary>
    /// Locks the rows of <typeparamref name="TEntity"/> whose <paramref name="column"/> equals
    /// <paramref name="value"/> until the current transaction ends (<c>SELECT ... FOR UPDATE</c>);
    /// another transaction locking them waits, then sees what this one committed. Locking a row
    /// that does not exist locks nothing.
    /// </summary>
    public static Task LockForUpdateAsync<TEntity, TValue>(
        this DbContext context, Expression<Func<TEntity, TValue>> column, TValue value, CancellationToken cancellationToken = default)
        where TEntity : class
        where TValue : notnull
    {
        var sql = context.Sql<TEntity>();
        var name = sql.Column(column);
        return context.Database.ExecuteSqlAsync(
            FormattableStringFactory.Create($"SELECT 1 FROM {sql.Table} WHERE {name} = {{0}} FOR UPDATE", value), cancellationToken);
    }

    /// <summary>
    /// Locks every row whose <paramref name="column"/> is among <paramref name="values"/>, in the
    /// column's order, so two transactions locking overlapping sets cannot each hold a row the
    /// other waits for.
    /// </summary>
    public static Task LockAllForUpdateAsync<TEntity, TValue>(
        this DbContext context, Expression<Func<TEntity, TValue>> column, IEnumerable<TValue> values, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var sql = context.Sql<TEntity>();
        var name = sql.Column(column);
        return context.Database.ExecuteSqlAsync(
            FormattableStringFactory.Create($"SELECT 1 FROM {sql.Table} WHERE {name} = ANY({{0}}) ORDER BY {name} FOR UPDATE", values.Distinct().ToArray()),
            cancellationToken);
    }
}
