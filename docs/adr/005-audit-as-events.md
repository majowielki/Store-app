# 005 - The audit trail is a stream of events

**Status**: accepted

## Context

The first version audited through an HTTP middleware that, on every request, called the audit
service with the user's token: two extra HTTP calls on the path of every request, personal data
(addresses, e-mails, headers) in the log, an audit service that was a dependency of everything,
and correlation ids that were generated per service and never passed on.

## Decision

An audited action is a business event. Each service records through `IAuditTrail`, which
publishes an `AuditEvent` (action, entity, identifiers, a redacted before/after) through the
service's outbox in the same transaction as the action; the audit service consumes it, stores
it and keeps it for 90 days. Request-level correlation is OpenTelemetry's job: the
`traceparent` travels with the HTTP calls and the messages, and the `traceId` in a problem
response is the one in the traces.

## Consequences

- Nothing on the request path waits for the audit; a slow or absent audit service is a delayed
  audit, not a slow store.
- What the audit holds is the fact, with identifiers - not the payload. Anything needed to
  reconstruct a request is in the traces, which carry no bodies, headers or secrets either.
- Partitioning the audit table by month is deferred until its size asks for it; retention alone
  keeps it small at the current volume.
