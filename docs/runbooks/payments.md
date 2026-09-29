# Payments

A simulated card provider (`Services/PaymentService`, database `store_payment_db`,
[ADR 011](../adr/011-simulated-payments.md)) and the order saga that waits for it
([ADR 013](../adr/013-order-saga.md)). Nothing is charged: only the test cards are taken
(`GET /api/v1/payments/test-cards` lists them with their outcomes).

## How a payment reaches the order

1. Once the stock is reserved the order is `AwaitingPayment`, due in 15 minutes
   (`OrderSaga:PaymentWindowMinutes`). The payment page opens the order's payment through the
   order service (`POST /api/v1/orders/{id}/payment`, one payment per order, idempotency key
   `order-{id}`).
2. The customer confirms it with a card (`POST /api/v1/payments/{id}/confirm`), and approves
   3-D Secure when the card asks for it. A refused card leaves the payment waiting for another one.
3. The payment service records a webhook (`WebhookDeliveries`) and posts it to the order service
   (`POST /api/v1/webhooks/payments`, internal network only, not routed by the gateway), signed with
   HMAC-SHA256 over `timestamp.body` with `PAYMENT_WEBHOOK_SECRET`. The order service checks the
   signature and the timestamp (5 minutes' tolerance), remembers the event id for 7 days
   (`ProcessedWebhookEvents`) and publishes `PaymentAccepted`; the saga marks the order `Paid`.
4. An order still unpaid at its deadline is cancelled (`payment-timed-out`), its stock released and
   its payment cancelled. A payment that succeeds after that is refunded.

## A payment that went through but the order is not paid

The webhook has not been taken yet. The dispatcher retries after 1, 5, 30 and 30 minutes
(`PaymentWebhooks:RetryDelaysMinutes`), five attempts in all, then gives up:

```sql
-- store_payment_db: webhooks not delivered yet, or given up
SELECT "Id", "PaymentId", "Type", "Attempts", "NextAttemptAt", "FailedAt", "LastError"
FROM "WebhookDeliveries" WHERE "DeliveredAt" IS NULL ORDER BY "NextAttemptAt";
```

- `LastError` 401: the two services do not share the secret. `PAYMENT_WEBHOOK_SECRET` (Key Vault
  secret `payment-webhook-secret` in Azure) must be the same for both; after fixing it, restart
  the payment service's revision and the next attempt goes through.
- `LastError` a timeout or a 5xx: the order service was down or failing; its log and the trace of
  the `POST /api/v1/webhooks/payments` request say why.
- A webhook given up (`FailedAt` set) is sent again by clearing it; the event id stays the same, so
  the order service ignores it if it had taken it after all:

  ```sql
  UPDATE "WebhookDeliveries" SET "FailedAt" = NULL, "NextAttemptAt" = now() WHERE "Id" = '<id>';
  ```

A payment made after the order's deadline is refunded on its own by the saga; nothing to do.

## Rotating the webhook secret

The secret is read at start. Set the new value in Key Vault (`payment-webhook-secret`) or `.env`,
then restart the order service first and the payment service right after: webhooks signed with
the old secret in between fail with 401 and are retried a minute later with the new one.

## The card data

Only the brand and the last four digits are stored (`PaymentAttempts`); the card number, the expiry
and the code are neither stored nor logged. Refunds are a status change of the payment and a
`PaymentRefunded` webhook: the saga moves the order to `Refunded`.
