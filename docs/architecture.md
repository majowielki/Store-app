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
    CA -->|product snapshots<br/>typed client, internal key| PR
    OR -->|cart snapshot| CA
    OR -->|product snapshots| PR
  end
  subgraph MassTransit + RabbitMQ, outbox and inbox in every database
    OR -- OrderPlaced --> MQ[(RabbitMQ)]
    MQ -- OrderPlaced --> CA
    MQ -- OrderPlaced --> ID
    MQ -- OrderPlaced --> AU
    ID & PR & CA & OR -- AuditEvent --> MQ
    MQ -- AuditEvent --> AU
  end
  GW & ID & PR & CA & OR & AU -.OTLP: traces, metrics, logs.-> OT[Aspire dashboard / Azure Monitor]
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
   returns the order created before), the totals are computed by `PricingPolicy`, the order and
   the key are written and `OrderPlaced` goes into the outbox.
4. The outbox delivers `OrderPlaced` to RabbitMQ; the cart service empties the cart, the identity
   service stores the delivery address when asked to, the audit service records the order. Each
   consumer's inbox makes a redelivery harmless.
5. The UI, having received 201, empties its cached cart and updates the profile at once rather
   than waiting for the events.

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
