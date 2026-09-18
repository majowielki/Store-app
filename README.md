# Store

A small furniture shop built as a set of .NET 9 services behind an API gateway, with a React
front end. It exists to exercise the patterns a distributed system needs - and to show them
working end to end: an API contract with generated clients, sessions with rotating refresh
tokens, a transactional outbox between services, idempotent checkout, telemetry across every
hop, non-root images, one-command local stack, infrastructure as code and a deployment
pipeline that migrates before it deploys.

| Part | What it does | Where |
|------|--------------|-------|
| **Gateway** | the one public entry point: routes `/api/v1/*` to the services (YARP), validates the bearer token, applies the authorization policies and the per-client rate limits, sets the API's security headers | `Gateway/APIGateway` |
| **Identity** | accounts, sign-in, 15-minute access tokens and rotating refresh tokens in an httpOnly cookie, the admin's user listing, the demo accounts | `Services/IdentityService` |
| **Catalogue** | products, filters, the admin's product management (soft delete), product snapshots for the other services | `Services/ProductService` |
| **Cart** | the signed-in customer's cart, priced from the catalogue and refreshed when it is read; the guest cart merges into it at sign-in | `Services/CartService` |
| **Orders** | checkout from the server cart with idempotency keys and the pricing rules (delivery, first-order discount), the customer's orders, the admin's order views and statistics | `Services/OrderService` |
| **Audit** | the audit trail every service publishes as events, with a 90-day retention | `Services/AuditLogService` |
| **UI** | React 18 + TypeScript SPA: shop, cart, checkout, orders, admin panel; RTK Query over the generated API types | `UI/store-app.UI` |
| **Shared** | `Store.Contracts` (events, snapshots, roles - data only) and `Store.BuildingBlocks` (auth, problem details, messaging, health, observability - the plumbing every host composes) | `Shared/` |

The services own their data (one PostgreSQL database each) and talk to each other in two ways:
synchronously through typed HTTP clients with retries and circuit breakers (cart → catalogue,
orders → cart and catalogue), and asynchronously through RabbitMQ with MassTransit and a
transactional outbox/inbox (`OrderPlaced` empties the cart, stores the address, feeds the audit;
every audited action is an `AuditEvent`). [docs/architecture.md](docs/architecture.md) has the
picture and the reasoning; [docs/adr](docs/adr) the decisions.

## Running it

**Everything in containers** - copy `.env.example` to `.env`, fill in the secrets, then

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

gives the shop on <http://localhost:8081> (`FRONTEND_PORT`), the gateway on 5000, the demo accounts
(the *Demo User* / *Demo Admin* buttons on the sign-in page), Swagger on every service and the
Aspire dashboard with traces, logs and metrics on <http://localhost:18888>. Without the dev
override the same command runs the stack the way production does (see
[docs/DOCKER_COMPOSE.md](docs/DOCKER_COMPOSE.md)).

**Services from the IDE** - start only PostgreSQL and RabbitMQ from compose, put the secrets into
user secrets with `Scripts/Set-Local-Secrets.ps1`, run each host with
`ASPNETCORE_ENVIRONMENT=Development`, the gateway with the cluster addresses as arguments, and
the UI with `npm run dev`; the details are in the same document.

## Checking it

| What | How | Where it runs |
|------|-----|---------------|
| build with warnings as errors, format | `dotnet build Store.Microservices.slnx`, `dotnet format --verify-no-changes` | CI `backend` |
| unit tests | `dotnet test Tests/Unit` | CI |
| integration tests: every service in-process against PostgreSQL in Testcontainers, the bus on the in-memory transport, the other services faked | `dotnet test Tests/Integration` (needs Docker) | CI |
| the OpenAPI documents in `docs/api/openapi` match the code | `Scripts/Export-OpenApi.ps1 -Check` | CI |
| UI: lint, types, unit and component tests (Vitest, Testing Library, MSW) | `npm run lint`, `npx tsc -b`, `npm test` in `UI/store-app.UI` | CI `frontend` |
| the generated API types match the documents | `npm run api:check` | CI |
| end to end (Playwright): guest browsing, registration with cart merge, checkout, sale prices, the admin's product management, the demo admin's read-only access | `npm run e2e` against a running stack | workflow `End-to-end` (nightly, on demand, release tags) against `docker compose` |
| the infrastructure template compiles | `az bicep build` | CI `infra` |
| images build | every Dockerfile | CI `docker` |

`docs/api/store.http` walks the whole API by hand (REST Client / Rider).

## Deploying it

`infra/bicep/main.bicep` describes the store on Azure Container Apps and
`.github/workflows/cd.yml` deploys it: images built in the registry, each service's migrations
run as a job, then the new revisions, then a smoke test. The one-time preparation and the
runbooks (restore, key rotation, dead letters) are in [docs/runbooks](docs/runbooks);
[docs/observability.md](docs/observability.md) says where the telemetry goes and which alerts to
set.

## Conventions

- **API**: `/api/v1`, plain DTOs, every failure an `application/problem+json` with the right status
  (404, 403, 409, 422 for anything invalid) and a `traceId`; listings share `PagedResponse`;
  the OpenAPI documents are committed and the UI types are generated from them.
- **Configuration**: validated options classes (`ValidateOnStart`), secrets only from the
  environment or user secrets, never in a file that is committed or copied into an image.
- **Data**: migrations are code, one `InitialSchema` per service plus what came after; a service
  outside Development refuses to start on a schema it does not know, `--migrate` applies the
  migrations and the seed and exits.
- **Code**: nullable and analyzers on, warnings are errors, `dotnet format` clean; central package
  versions in `Directory.Packages.props`; comments say why, the names say what.
- **Commits**: lowercase imperative subject, a body in prose explaining the change.
