# 013 - An order is a saga

**Status**: accepted (amended with the implementation: a refused card no longer cancels the order)

## Context

With stock and payments, an order passes through steps that other services own: the product
service reserves the stock, the payment service takes the money, the administrator ships.
Before, the order service created the order synchronously from the cart and it stayed "placed";
nothing coordinated what happened next or undid a step when a later one failed.

## Decision

A MassTransit state machine in the order service (`OrderStateMachine`), stored with Entity
Framework in an `OrderStates` table next to the orders, locked per order while it handles a
message and publishing through the existing outbox (002). An order is submitted (still the
synchronous request with its idempotency key) and its saga row is written in the same
transaction, so every later event finds it. The order waits for the stock reservation, then for
the payment, and is paid once the payment webhook arrives (011); the administrator marks it
shipped.

- A missing product cancels the order; nothing was reserved (the stock reserves all lines or none).
- A refused card does not cancel it: the customer may try another card until the deadline.
- A payment not completed within 15 minutes of the reservation cancels the order and releases
  the stock. The deadline is checked by a background job reading the saga rows every 30 seconds,
  not by a delayed message, so RabbitMQ needs no plugin and a restart loses nothing.
- The administrator changes an order by hand only to ship a paid one or to cancel one that is not
  shipped. Cancelling a paid order asks the payment service for a refund; a payment that arrives
  for an order already cancelled is refunded the same way. The refund moves the order to refunded.

Every status change goes through one writer that locks the order row and checks the move against
`OrderStatusFlow`, whether the saga or the administrator makes it, so a payment and a
cancellation arriving together are applied one after the other and the later one sees the
earlier. Each change is recorded with its time for the customer's order timeline and published
as `OrderStatusChanged` for the audit.

## Consequences

- The shop is eventually consistent: after checkout the order is "placed" for a moment while the
  stock is reserved, and the payment page waits for "awaiting payment" before it takes a card.
- Each path (paid, out of stock, declined then timed out, timed out, refunded, paid after the
  deadline, paid while the administrator cancels) has a saga test on the MassTransit test
  harness against PostgreSQL.
- Payment is the saga's business; the admin panel no longer has "mark as paid".
