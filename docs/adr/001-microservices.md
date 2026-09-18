# 001 - Separate services, one database each

**Status**: accepted

## Context

The store is a portfolio project: its purpose is to show how a distributed system is built and
operated, not to serve a load that a single process could not. The first version had the shape
of microservices without their substance: one shared library holding every entity, services
reading each other's tables through a common model, a gateway that anybody could bypass.

A modular monolith would have been simpler and, for the traffic involved, better. It would also
have removed most of what the project is meant to demonstrate.

## Decision

Keep separate services - identity, catalogue, cart, orders, audit - behind one gateway, and make
the separation real: each service owns its entities and its schema, nothing but contracts
(`Store.Contracts`: DTOs, snapshots, events) leaves a service, calls between services go through
typed clients or events, the services are unreachable from outside. The cross-cutting plumbing is
one library (`Store.BuildingBlocks`) so the six hosts stay alike.

## Consequences

- Every pattern the shape demands has to be present and working: resilient clients, outbox and
  inbox, idempotent checkout, distributed tracing, per-service migrations, internal ingress.
  These are the point of the project, so the cost is the deliverable.
- Data that two services need is copied as a snapshot (the cart holds the product's title, image
  and price at the time it was added) and refreshed deliberately, not read across the boundary.
- Local development needs the whole stack; `docker compose` provides it in one command.
