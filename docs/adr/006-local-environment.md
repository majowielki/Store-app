# 006 - Docker Compose for the local stack, not an Aspire AppHost

**Status**: accepted

## Context

Six hosts, PostgreSQL and RabbitMQ have to run together for the store to work. The choice was
between a .NET Aspire AppHost (orchestration in C#, its dashboard for free) and Docker Compose
(orchestration in YAML, the same images as production).

## Decision

Docker Compose, in two layers: `docker-compose.yml` runs the store the way production does
(Production environment, chiseled non-root images, migrations as one-shot containers before each
service, no default password, no infrastructure port on the host, resource limits, no service
waiting for another), and `docker-compose.dev.yml` adds what a developer wants (Development,
demo accounts, host ports, the Aspire dashboard as the OTLP target). The Aspire dashboard is
used - as a container, for the telemetry - without the AppHost.

## Consequences

- What runs locally is what runs in the pipeline and what the images are; the end-to-end tests
  run against the same compose in CI.
- Working on one service from the IDE means starting the rest from compose and the service by
  hand with its environment set; `docs/DOCKER_COMPOSE.md` describes it.
- An AppHost can be added later for the inner loop without changing anything here; the
  container images and the compose files stay the definition of the stack.
