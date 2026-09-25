# 013 - An order is a saga

**Status**: accepted

## Context

With stock and payments, an order passes through steps that other services own: the product
service reserves the stock, the payment service takes the money, the administrator ships.
Today the order service creates the order synchronously from the cart and it stays "placed";
nothing coordinates what happens next or undoes a step when a later one fails.

## Decision

A MassTransit state machine in the order service, stored with Entity Framework next to the
orders and publishing through the existing outbox (002). An order is submitted (still the
synchronous request with its idempotency key), waits for the stock reservation, then for the
payment, and is paid once the payment webhook arrives (011); the administrator marks it shipped.
A missing product or a failed payment cancels it and releases the stock; a payment not completed
within 15 minutes cancels it too. That deadline is checked by a background job reading the
orders, not by a delayed message, so RabbitMQ needs no plugin. A refund moves a paid order to
refunded. Every change of state is recorded with its time for the customer's order timeline.

## Consequences

- The shop becomes eventually consistent: the checkout shows "processing" until the events
  arrive, and the order page follows the state.
- Each path (paid, out of stock, declined, timed out, refunded) has a saga test on the
  MassTransit test harness.
- The administrator's manual status change is limited to shipping and cancelling; payment is
  the saga's business.
