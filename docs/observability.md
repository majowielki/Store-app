# Observability

Every host (the gateway and the five services) is instrumented with OpenTelemetry through
`Store.BuildingBlocks.Observability.AddStoreObservability(serviceName)`:

- **Traces** – one trace per request across the gateway, the service, PostgreSQL (Npgsql) and
  the bus (MassTransit propagates the W3C `traceparent` with every message, so a consumer's
  work hangs under the request that published the event). Health probes are not traced, and the
  database polls of the outbox delivery are dropped (`BackgroundDatabaseSpanFilter`), so a
  trace is always something a client or a message started. The signed-in user is on the request
  span as `enduser.id`; the token never is.
- **Metrics** – ASP.NET Core, HttpClient, the runtime, MassTransit and the store's own meter
  (below).
- **Logs** – the ordinary `ILogger` output, structured, with the trace and span ids of the
  request that produced them.

Everything leaves through OTLP to `OTEL_EXPORTER_OTLP_ENDPOINT`. Without the variable nothing is
exported and nothing else changes. The `traceId` in every problem response is the same id, so a
user's error report can be found in the traces.

## Where to look

| Environment | Backend | How |
|-------------|---------|-----|
| `docker compose` | Aspire dashboard | <http://localhost:18888> (traces, structured logs, metrics per resource); the services export to `http://aspire-dashboard:18889` |
| services run by hand | Aspire dashboard | `docker run --rm -p 18888:18888 -p 18889:18889 -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true mcr.microsoft.com/dotnet/aspire-dashboard:13.5` and `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:18889` for each host |
| Azure Container Apps | Azure Monitor / Application Insights | the environment's managed OpenTelemetry agent receives OTLP from the apps and forwards it: `az containerapp env telemetry app-insights set --name <env> --resource-group <rg> --connection-string <ai-connection-string> --enable-open-telemetry-traces true --enable-open-telemetry-logs true` (and `telemetry otlp add` for metrics to another OTLP backend); every app gets `OTEL_EXPORTER_OTLP_ENDPOINT` from the environment, no SDK of the vendor is compiled in |

The dashboard keeps nothing across restarts; it is a window, not a store.

## The store's metrics (`Store` meter)

| Metric | Type | Recorded by | Tags | Meaning |
|--------|------|-------------|------|---------|
| `store.orders.placed` | counter | order service | `discount` = `first-order` / `none` | orders placed |
| `store.orders.value` | histogram (USD) | order service | `discount` | what customers paid per order |
| `store.cart.items.added` | counter | cart service | – | pieces added to carts |
| `store.auth.login.failed` | counter | identity service | `reason` = `unknown-account` / `inactive` / `wrong-password` / `locked-out` | sign-ins refused (never the e-mail or the address) |
| `store.catalog.price_mismatch` | counter | cart and order services | `where` = `cart` / `checkout` | cart lines whose stored price differed from the catalogue when refreshed or ordered |
| `store.outbox.pending` | gauge | every service with a bus | – | outbox messages the delivery service has not handed to the broker yet (counted every 30 s) |

Plus what the libraries provide: `http.server.request.duration` (ASP.NET Core, per route and
status), `http.client.request.duration`, `messaging.*` (MassTransit: consume duration, faults),
`dotnet.*` (GC, thread pool, exceptions), `db.client.*` when Npgsql metrics are enabled.

## Alerts

The minimal set, as queries against Application Insights / Log Analytics (Container Apps). The
first four should page; the last two can wait for the morning.

| # | Condition | Query (KQL) | Window |
|---|-----------|-------------|--------|
| 1 | a service is not ready | Container Apps: `ContainerAppSystemLogs_CL \| where Reason_s == "UnhealthyReplica"` – or probe the gateway's `/health/ready` from an availability test; the readiness endpoint answers 503 while a dependency is down | `Unhealthy` for > 2 min |
| 2 | the gateway answers 5xx | `requests \| where cloud_RoleName == "gateway" \| summarize failed = countif(resultCode startswith "5"), total = count() by bin(timestamp, 5m) \| where total > 20 and todouble(failed) / total > 0.01` | > 1 % over 5 min |
| 3 | events are not leaving a service | `customMetrics \| where name == "store.outbox.pending" \| summarize max(value) by cloud_RoleName, bin(timestamp, 1m) \| where max_value > 100` | > 100 for 10 min |
| 4 | poison messages | RabbitMQ: length of any `*_error` queue > 0 (`rabbitmqctl list_queues name messages \| grep _error`, or the management API `/api/queues` scraped into the metrics backend) | > 0 |
| 5 | checkout is slow | `requests \| where cloud_RoleName == "order" and name == "POST /api/v1/orders/from-cart" \| summarize percentile(duration, 95) by bin(timestamp, 5m) \| where percentile_duration_95 > 2000` | p95 > 2 s over 5 min |
| 6 | somebody is guessing passwords | `customMetrics \| where name == "store.auth.login.failed" \| summarize sum(valueSum) by bin(timestamp, 1m) \| where sum_valueSum > 50` – per address, take the gateway's 429s on `/api/v1/auth/login` (`requests \| where resultCode == "429"`) since the metric carries no address on purpose | > 50 / min |

The same conditions read the same on any OTLP backend; only the query language differs.

## What is deliberately not recorded

- request and response bodies, `Authorization` headers, cookies;
- SQL parameters (Npgsql records the statement text, EF Core's own logging is off outside
  Development);
- e-mail addresses in metric tags (they are high-cardinality and personal); the audit trail is
  where per-user actions are kept.
