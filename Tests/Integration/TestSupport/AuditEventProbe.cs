using MassTransit;
using Store.Contracts.Audit.V1;

namespace Store.Tests.Integration.TestSupport;

/// <summary>
/// Consumes the audit events the service under test publishes, so tests can observe them
/// through the harness. Events published from inside a consumer go through the consumer
/// outbox, which the harness does not report as Published - but it does report what this
/// probe consumed.
/// </summary>
public sealed class AuditEventProbe : IConsumer<AuditEvent>
{
    public Task Consume(ConsumeContext<AuditEvent> context) => Task.CompletedTask;
}
