# Running the store with Docker Compose

Three files in the repository root; the first is the base, the others are overrides:

| File | What it adds |
|------|--------------|
| `docker-compose.yml` | the whole store the way it runs in production: `Production` environment, the UI on <http://localhost:8081> and nothing else on the host, no default passwords, CPU and memory limits, migrations as one-shot containers |
| `docker-compose.dev.yml` | `Development` environment (Swagger per service), the demo accounts, every port on the host (gateway 5000, services 5001–5006, PostgreSQL 5432, RabbitMQ 5672 / 15672) and the Aspire dashboard on <http://localhost:18888> |
| `docker-compose.tools.yml` | pgAdmin on <http://localhost:8080>, behind the `tools` profile |

```bash
cp .env.example .env            # then fill in every empty value - compose refuses to start otherwise
docker compose up --build       # prod-like
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build            # development
docker compose --profile tools -f docker-compose.yml -f docker-compose.dev.yml -f docker-compose.tools.yml up
```

`--build` matters: the migration containers reuse the image of their service (`store/identity`
and so on) and there is nothing to pull.

## What happens on `up`

1. PostgreSQL (with one database per service, created by `Infrastructure/scripts/init-databases.sql`
   on the first start) and RabbitMQ come up and pass their health checks.
2. For each service a one-shot container runs the same image with `--migrate`: it applies the
   migrations checked into the repository and the seed (catalogue, roles, the true administrator
   from `TRUE_ADMIN_PASSWORD`, the demo accounts when `DEMO_ENABLED=true`), then exits.
3. The services start once their migration finished. In `Production` a service refuses to start
   against a schema it does not know, so a failed migration stops the service, not the data.
4. The gateway starts on its own - it probes the services every ten seconds and routes to the
   ones that answer - and the UI's nginx proxies `/api/` to it.

No service waits for another service: a slow or failing neighbour is handled by the retries and
circuit breakers of the typed clients, and events wait in the outbox until the broker takes them.

## Images

Every .NET host is built from the repository root (`Services/<Name>/Dockerfile`,
`Gateway/APIGateway/Dockerfile`) into a chiseled `aspnet:9.0-noble-chiseled` image: no shell, no
package manager, runs as the unprivileged `app` user, listens on 8080. The UI image
(`UI/store-app.UI/Dockerfile`) serves the bundle with `nginx-unprivileged` on 8080. Because the
chiseled images have no `curl`, the containers carry no health check; the orchestrator probes
`/health/ready` over HTTP (Container Apps probes, or the wait loop in the end-to-end workflow).

`.dockerignore` keeps `appsettings.Production.json`, `appsettings.*.local.json` and `.env` out of
every image: configuration and secrets reach a container only through its environment.

## Variables

See `.env.example`. Required: `POSTGRES_PASSWORD`, `RABBITMQ_PASSWORD`, `JWT_SECRET_KEY`,
`INTERNAL_API_KEY`, `TRUE_ADMIN_PASSWORD`. Optional: `ASPNETCORE_ENVIRONMENT` (the dev override
sets `Development`), `DEMO_ENABLED`, `AUTH_CREDENTIAL_LIMIT` (sign-ins per minute per client, raised
for the end-to-end tests), `OTEL_EXPORTER_OTLP_ENDPOINT`, the host ports.

## Data

Two named volumes, `store_postgres_data` and `store_rabbitmq_data`. `docker compose down -v`
removes them; the next `up` recreates the databases from the migrations and the seed. To reset a
single service's database while the stack is down, `Scripts/Reset-Local-Databases.ps1` does the
same with the dev override's PostgreSQL port.

## Running the services outside Docker

Start only the infrastructure (`docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d postgres rabbitmq`),
put the secrets into user secrets (`Scripts/Set-Local-Secrets.ps1`) and run each host with
`ASPNETCORE_ENVIRONMENT=Development` and its port (`dotnet run --project Services/OrderService --urls http://localhost:5006`);
the gateway takes the cluster addresses as command-line arguments
(`--ReverseProxy:Clusters:orders-cluster:Destinations:destination1:Address=http://localhost:5006/`).
The UI dev server (`npm run dev` in `UI/store-app.UI`) proxies `/api` to the gateway on 5000.
