# 002 - MassTransit over RabbitMQ with the outbox in each database

**Status**: accepted

## Context

Placing an order has side effects in other services: the cart must be emptied, the profile may
store the address, the audit must record it. The first version called the other services over
HTTP with the customer's token during checkout, so a cart service that was down failed the
order, and a token that had expired between two calls left the order half done. A hand-written
RabbitMQ wrapper existed but was not used for anything that mattered.

The options were a hand-written bus, Wolverine or MassTransit.

## Decision

MassTransit with the RabbitMQ transport and its Entity Framework outbox and inbox in each
service's own database. A service publishes inside its unit of work: the event is written next
to the data it describes and a delivery service hands it to the broker afterwards. Consumers run
inside the inbox, so a redelivered message they have already handled is dropped; a message that
fails three attempts with exponential back-off goes to the `_error` queue next to its own.
Every service has its own queue per event (`cart-order-placed`, `identity-order-placed`, ...),
because two services sharing a queue would each get half of the messages.

## Consequences

- Checkout no longer depends on the other services being up; a broker that is down delays events
  and loses none. The trade is eventual consistency: the cart empties a moment after the order,
  and the UI updates its own view immediately rather than waiting.
- The bus is also the audit's transport (see 005) and carries the W3C trace context, so a
  consumer's work appears under the request that published the event.
- MassTransit 8.5 is Apache-licensed; the version is pinned centrally and moved deliberately.
- The outbox needs a running delivery loop per service; its polling is filtered out of the traces
  and its backlog is a metric (`store.outbox.pending`) with an alert.
