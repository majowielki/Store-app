# Running the store with Docker Compose

Three files in the repository root; the first is the base, the others are overrides:

| File | What it adds |
|------|--------------|
| `docker-compose.yml` | the whole store the way it runs in production: `Production` environment, the UI on <http://localhost:8081> and the pictures (Azurite) on 10000, nothing else on the host, no default passwords, CPU and memory limits, migrations as one-shot containers |
| `docker-compose.dev.yml` | `Development` environment (Swagger per service), the demo accounts, every port on the host (gateway 5000, services 5001–5010, PostgreSQL 5432, RabbitMQ 5672 / 15672), the Aspire dashboard on <http://localhost:18888> and Mailpit on <http://localhost:8025> catching the e-mails |
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

## Product pictures

The catalogue points at `http://localhost:10000/devstoreaccount1/product-images/<name>.webp`.
Azurite serves them in both setups, the way Blob Storage does in Azure: on every `up` the one-shot
`blobs-seed` container uploads the WebP files from `Blobs/` with their smaller copies from
`Blobs/w*/` (ADR 016), and replaces what is there, so a regenerated picture shows up without
resetting the volume. The repository keeps only these WebP files; the generated originals stay
in `Blobs/` on the machine that made them, ignored by git. A new or regenerated picture goes
through

```bash
dotnet run Scripts/optimize-images.cs     # JPEG/PNG in Blobs/ -> WebP, 1600 px (products) or 2000 px (covers)
dotnet run Scripts/make-image-sizes.cs    # the smaller copies the UI asks for by size: w400, w800, w1200, w32
```

and `Scripts/Upload-Blobs.ps1 -AccountName <account>` does the upload for a real storage account.

## Images

Every .NET host is built from the repository root (`Services/<Name>/Dockerfile`,
`Gateway/APIGateway/Dockerfile`) into a chiseled `aspnet:10.0-noble-chiseled` image: no shell, no
package manager, runs as the unprivileged `app` user, listens on 8080. The UI image
(`UI/store-app.UI/Dockerfile`) serves the bundle with `nginx-unprivileged` on 8080. Because the
chiseled images have no `curl`, the containers carry no health check; the orchestrator probes
`/health/ready` over HTTP (Container Apps probes, or the wait loop in the end-to-end workflow).

`.dockerignore` keeps `appsettings.Production.json`, `appsettings.*.local.json` and `.env` out of
every image: configuration and secrets reach a container only through its environment.

## Variables

See `.env.example`. Required: `POSTGRES_PASSWORD`, `RABBITMQ_PASSWORD`, `JWT_SECRET_KEY`,
`INTERNAL_API_KEY`, `TRUE_ADMIN_PASSWORD`. Optional: `ASPNETCORE_ENVIRONMENT` (the dev override
sets `Development`), `DEMO_ENABLED`, `AUTH_CREDENTIAL_LIMIT` (sign-ins per minute per client) and `AUTH_PERMIT_LIMIT`
(other /auth calls, such as the session renewal on every page load; both raised for the end-to-end
tests), `OTEL_EXPORTER_OTLP_ENDPOINT`, the host ports.

## Data

Three named volumes, `store_postgres_data`, `store_rabbitmq_data` and, with the dev override,
`store_azurite_data`. `docker compose down -v`
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
