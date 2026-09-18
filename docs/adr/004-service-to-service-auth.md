# 004 - An internal API key for calls between services

**Status**: accepted (a managed identity between Container Apps remains the destination)

## Context

The order service reads the cart and the catalogue, the cart service reads the catalogue. The
first version forwarded the customer's access token on these calls, so a call was only as valid
as the token that happened to be in the request - impossible from a consumer, wrong in
principle: the caller is the service, not the user.

The options were a service JWT (client credentials from the identity service), mutual TLS, a
managed identity (Azure only) or a shared internal key.

## Decision

A shared internal key (`InternalApi:ApiKey`, a validated option, from the environment) presented
by the typed clients and checked by a dedicated authentication handler on the internal endpoints
(`/api/v1/products/{id}/snapshot`, `/api/v1/cart/internal/{userId}`). Those endpoints are not
routed by the gateway, and the services have internal ingress, so the key is the second wall,
not the first.

## Consequences

- Simple, testable, works the same in compose and in the cloud; one secret to rotate, the same
  way as the signing key.
- The key does not say *which* service calls; when that matters, or when the platform makes it
  free, a managed identity per app (Container Apps) or a client-credentials token from the
  identity service replaces it without changing the endpoints.
