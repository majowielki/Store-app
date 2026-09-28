# 011 - Payments go through a simulated payment provider

**Status**: accepted (a real provider in test mode can replace it behind the same interface)

## Context

An order is paid for by nobody today: it is placed and stays placed. The shop should show how
a card payment is integrated - the redirect or challenge step, the asynchronous result, refunds
- but it runs as a public demo, without an account at a payment provider, keys to protect or
real money, and card numbers must never reach the order service.

## Decision

A payment service that behaves like a card provider in test mode. The order service creates a
payment (an internal call, 004, with an idempotency key); the browser confirms it with a test
card straight at the payment service, which answers some cards with a 3-D Secure step. The
payment service keeps the card's brand and last four digits only, and reports the outcome to the
order service with a webhook signed with HMAC-SHA256 over a timestamp and the body; the order
service rejects a stale or wrong signature and ignores an event id it has already processed.
Undelivered webhooks are retried after 1, 5, 30 and 30 minutes, five attempts at most (the
delays are `PaymentWebhooks:RetryDelaysMinutes`). Refunds go the same way: the order saga asks
for one with an event the payment service consumes (a cancelled order cancels its open payment
the same way) and the refund webhook confirms it; a real provider would get these requests
through its API. A refused card leaves the payment open for another
card. The test cards follow the numbers of Stripe's test mode (4242 4242 4242 4242 pays,
4000 0000 0000 3220 asks for 3-D Secure, 4000 0000 0000 9995 has no funds, 4000 0000 0000 0002 is
declined), and any other number is refused, so nobody can type a real card into the demo. The
payment page lists them from `GET /api/v1/payments/test-cards`, so the list lives in the provider
only.

## Consequences

- No external dependency or secret beyond the webhook signing key; the whole flow runs in compose.
- Webhook security, idempotency and retries are exercised for real, with tests; only the
  provider behind the `IPaymentProvider` interface is simulated.
- The payment service holds no card data, so the shop is not in scope of card-data rules; the
  same would hold with a real provider's hosted fields.
