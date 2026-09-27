# Architecture decision records

One file per decision, numbered in the order they were taken; a decision that replaces an
earlier one says so instead of editing it. Each record states the context, the decision and
what it costs.

| # | Decision |
|---|----------|
| [001](001-microservices.md) | Separate services, one database each |
| [002](002-messaging-masstransit.md) | MassTransit over RabbitMQ with the outbox in each database |
| [003](003-api-contract.md) | Plain DTOs and RFC 9457 problems; the OpenAPI document is the contract |
| [004](004-service-to-service-auth.md) | An internal API key for calls between services |
| [005](005-audit-as-events.md) | The audit trail is a stream of events |
| [006](006-local-environment.md) | Docker Compose for the local stack, not an Aspire AppHost |
| [007](007-demo-admin-masking.md) | The demo administrator sees masked data and may change nothing |
| [008](008-demo-accounts.md) | Demo accounts exist only where they are switched on |
| [009](009-session-tokens.md) | Access token in memory, refresh token in an httpOnly cookie |
| [010](010-content-service.md) | The shop's content lives in its own service |
| [011](011-simulated-payments.md) | Payments go through a simulated payment provider |
| [012](012-moderated-reviews.md) | Reviews are published only after moderation |
| [013](013-order-saga.md) | An order is a saga |
| [014](014-product-slugs.md) | Content names products by slug, not by id |
