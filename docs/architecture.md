# Architecture

## The shape

```mermaid
flowchart LR
  UI[React SPA<br/>nginx, same-origin /api] -->|HTTPS| GW[Gateway - YARP<br/>JWT, policies, per-client rate limits,<br/>forwarded and security headers]
  subgraph Internal ingress only
    GW --> ID[Identity]
    GW --> PR[Catalogue]
    GW --> CA[Cart]
    GW --> OR[Orders]
    GW --> AU[Audit read API]
    GW --> CO[Content]
    GW --> PA[Payments]
    CA -->|product snapshots<br/>typed client, internal key| PR
    OR -->|cart snapshot| CA
    OR -->|product snapshots| PR
    OR -->|open a payment| PA
    PA -.->|signed webhooks| OR
  end
  subgraph MassTransit + RabbitMQ, outbox and inbox in every database
    OR -- OrderPlaced --> MQ[(RabbitMQ)]
    MQ -- OrderPlaced --> CA
    MQ -- OrderPlaced --> ID
    MQ -- OrderPlaced --> AU
    MQ -- OrderPlaced, OrderCancelled, OrderShipped --> PR
    PR -- StockReserved, StockUnavailable --> MQ
    MQ -- StockReserved, StockUnavailable --> OR
    OR -- PaymentAccepted, PaymentRefunded --> MQ
    MQ -- OrderCancelled, PaymentRefundRequested --> PA
    OR -- OrderStatusChanged --> MQ
    MQ -- OrderStatusChanged --> AU
    ID & PR & CA & OR & CO & PA -- AuditEvent --> MQ
    MQ -- AuditEvent --> AU
  end
  GW & ID & PR & CA & OR & AU & CO & PA -.OTLP: traces, metrics, logs.-> OT[Aspire dashboard / Azure Monitor]
```

Each service has its own PostgreSQL database and its own entities; what crosses a boundary is a
contract from `Store.Contracts` (a DTO, a snapshot, an event) and nothing else. The cross-cutting
plumbing - authentication, authorization policies, validated options, problem details, health,
messaging, telemetry - lives in `Store.BuildingBlocks` and every host composes it the same way.

## The rules the design follows

1. **One public entry point.** Only the gateway and the UI are reachable from outside; the
   services have internal ingress. The browser talks to the UI's origin, whose nginx proxies
   `/api` to the gateway, so there is no CORS to configure and the refresh cookie stays
   same-origin. Service-to-service calls carry the internal API key, not the user's token.
2. **Commands over HTTP, facts as events.** When a service needs an answer it calls a typed
   client (timeouts, retries, circuit breaker; addresses from configuration). When it only has
   something to announce it publishes an event through the outbox in its own database, in the
   same transaction as the data; consumers are wrapped in an inbox and can be retried. A broker
   that is down delays events, it does not lose them; a neighbour that is down degrades one
   feature, not the store.
3. **The order is priced by the server, once, at checkout.** The cart carries snapshots of the
   catalogue and refreshes them when read; the order service re-prices every line from the
   catalogue before charging, locks the customer row so only one checkout can be the first
   order, and keys the request by `Idempotency-Key` so a retry returns the same order. The UI
   previews the totals with the same rules, fetched from the API.
4. **Fail fast at start, degrade gracefully at run time.** Missing configuration stops the host;
   a pending migration stops the host outside Development; a dependency that is down turns
   `/health/ready` into 503 and the broker into `Degraded`, never into "healthy".
5. **The contract is explicit.** `/api/v1`, plain DTOs, RFC 9457 problems for every failure
   with a `traceId`, OpenAPI documents committed and checked in CI, UI types generated from them.
6. **Observable by default.** OpenTelemetry in every host; the `traceparent` travels with HTTP
   calls and with messages, so one request is one trace from the gateway to the consumer.
   Nothing personal in metric tags, nothing secret in logs or spans.
7. **Sessions without a token in storage.** A 15-minute access token in memory, a rotating refresh
   token in an httpOnly cookie, reuse of a spent refresh token revokes the whole family.

## How a checkout runs

1. The UI posts `POST /api/v1/orders/from-cart` with an `Idempotency-Key`; the gateway validates
   the token, applies the `api` rate limit and forwards it.
2. The order service reads the cart snapshot from the cart service and the current price of every
   product from the catalogue (over HTTP, before any transaction).
3. In one transaction: the customer row is locked, the idempotency key is checked (a duplicate
   returns the order created before), a discount code the customer typed is locked and checked
   (`DiscountCodePolicy`: dates, minimum, usage limit), the totals are computed by `PricingPolicy`
   (the larger of the first-order discount and the code, never both), the order and the key are
   written and `OrderPlaced` goes into the outbox.
4. The outbox delivers `OrderPlaced` to RabbitMQ; the cart service empties the cart, the identity
   service stores the delivery address when asked to, the audit service records the order. Each
   consumer's inbox makes a redelivery harmless.
5. The UI, having received 201, empties its cached cart and updates the profile at once rather
   than waiting for the events.

After that the order is a saga (ADR 013), whose row was written with the order in step 3. The
product service reserves every line or none under row locks and answers `StockReserved` or
`StockUnavailable`; the saga moves the order to "awaiting payment" with a 15-minute deadline, or
cancels it. A background job cancels the orders still unpaid after their deadline, which
releases their stock. The administrator only ships a paid order or cancels one that is not
shipped; cancelling a paid order asks for a refund. Whoever makes a change - saga or
administrator - goes through one writer that locks the order row, checks the move against
`OrderStatusFlow`, adds a dated row to the history (the customer's timeline) and publishes
`OrderStatusChanged` through the outbox. The delivery window the checkout promises comes from
`DeliveryPolicy` (cut-off hour, business days) and is served with the pricing rules.

## How a payment runs

The payment service plays a card provider in test mode (ADR 011), so the order service treats it
the way a shop treats Stripe: it never sees a card, and it believes only signed webhooks.

1. Once the order waits for its payment, the payment page asks the order service
   (`POST /api/v1/orders/{id}/payment`), which opens the payment at the payment service over the
   internal key with the Idempotency-Key `order-{id}`: every click and reload gets the same one.
2. The browser confirms it with a card straight at the payment service
   (`POST /api/v1/payments/{id}/confirm`, through the gateway). Only the test cards are taken; a
   3-D Secure card waits for the customer's answer in a second call. Only the brand and the last
   four digits are stored, and nothing logs the number.
3. The outcome - succeeded, failed, refunded - is written as a webhook in the same transaction and
   sent by a dispatcher: signed with HMAC-SHA256 over "timestamp.body", retried after 1, 5, 30
   and 30 minutes until the order service answers 2xx.
4. The order service's webhook endpoint is not routed by the gateway. It refuses a wrong or
   stale (5 minutes) signature with 401, records the event id with the outbox message it
   publishes (`PaymentAccepted`, `PaymentDeclined`, `PaymentRefunded`) and acknowledges a repeat
   without acting on it again. The saga takes it from there.

A cancelled order cancels its open payment; a refund is asked for with `PaymentRefundRequested`
and confirmed by the refund webhook.

## Who may do what

| Role | Gets |
|------|------|
| visitor | catalogue, pricing rules, a cart in the browser |
| `user` | the server cart, checkout, own orders and profile |
| `demo-admin` | every admin view with customers' data masked; every change refused with 403 |
| `true-admin` | everything |

The roles are claims in the access token; the gateway enforces the policy named on each route
(`User`, `Admin`) and the services enforce the finer ones (`AdminWrite`). The demo accounts and
the password-less demo logins exist only where `Demo:Enabled` is on.

## Where things run

| | Locally | Azure |
|-|---------|-------|
| hosts | `docker compose` (chiseled, non-root images) or the IDE | Container Apps, internal ingress for the services, external for the gateway and the UI ([infra/bicep](../infra/bicep)) |
| data | PostgreSQL and RabbitMQ containers | PostgreSQL flexible server; RabbitMQ as a single replica on Azure Files |
| secrets | `.env` / user secrets | Key Vault, referenced by the apps through a managed identity |
| telemetry | Aspire dashboard | the environment's OpenTelemetry agent into Application Insights |
| migrations | one-shot `<service>-migrate` containers before each service | Container Apps jobs run by the CD pipeline before the deployment |

The decisions behind the shape - and the ones that went the other way from the first version -
are recorded in [adr](adr).
