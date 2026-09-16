namespace Store.Contracts.Audit.V1;

/// <summary>
/// A business action worth keeping in the audit trail, published by the service that
/// performed it and stored by the audit service. Carries identifiers and business fields
/// only: no e-mail addresses, delivery addresses, headers, IPs or stack traces - the audit
/// database must not become the richest source of personal data in the system.
/// </summary>
/// <param name="Action">What happened, e.g. PRODUCT_UPDATED, CART_CLEARED</param>
/// <param name="EntityName">Kind of thing it happened to, e.g. Product, Cart</param>
/// <param name="EntityId">Its identifier, when there is one</param>
/// <param name="UserId">Who did it, when a user did</param>
/// <param name="ServiceName">Service that performed the action</param>
/// <param name="OccurredAt">When (UTC)</param>
/// <param name="Details">Small JSON object with business context (ids, amounts, counts)</param>
/// <param name="OldValues">JSON of the business fields before a change, for updates</param>
/// <param name="NewValues">JSON of the business fields after a change, for creates and updates</param>
public sealed record AuditEvent(
    string Action,
    string EntityName,
    string? EntityId,
    string? UserId,
    string ServiceName,
    DateTime OccurredAt,
    string? Details = null,
    string? OldValues = null,
    string? NewValues = null);
