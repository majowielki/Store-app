using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Audit.V1;
using System.Text.Json;

namespace Store.BuildingBlocks.Messaging;

/// <summary>
/// Records business actions for the audit service. The entry travels as an event through
/// the outbox, so it costs no request time and survives a broker outage; the audit service
/// stores it when it arrives.
/// </summary>
public interface IAuditTrail
{
    /// <summary>
    /// Records an action. <paramref name="details"/>, <paramref name="oldValues"/> and
    /// <paramref name="newValues"/> are serialised as JSON; pass identifiers and business
    /// fields, never personal data.
    /// </summary>
    Task RecordAsync(
        string action,
        string entityName,
        string? entityId,
        string? userId,
        object? details = null,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes <see cref="AuditEvent"/> through the bus outbox of <typeparamref name="TDbContext"/>.
/// The outbox stores a published message with the next SaveChanges, so this saves right away:
/// audit entries are usually recorded after the business change was already saved.
/// </summary>
public sealed class BusAuditTrail<TDbContext> : IAuditTrail where TDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TDbContext _context;
    private readonly string _serviceName;

    public BusAuditTrail(IPublishEndpoint publishEndpoint, TDbContext context, AuditTrailOptions options)
    {
        _publishEndpoint = publishEndpoint;
        _context = context;
        _serviceName = options.ServiceName;
    }

    public async Task RecordAsync(
        string action,
        string entityName,
        string? entityId,
        string? userId,
        object? details = null,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(new AuditEvent(
            action,
            entityName,
            entityId,
            userId,
            _serviceName,
            DateTime.UtcNow,
            Serialize(details),
            Serialize(oldValues),
            Serialize(newValues)), cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? Serialize(object? value)
        => value is null ? null : value as string ?? JsonSerializer.Serialize(value, JsonOptions);
}

/// <summary>Name under which a service signs its audit entries.</summary>
public sealed record AuditTrailOptions(string ServiceName);
