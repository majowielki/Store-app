# 003 - Plain DTOs and RFC 9457 problems; the OpenAPI document is the contract

**Status**: accepted

## Context

The first API wrapped some answers in an `ApiResponse<T>` envelope (`success`, `message`,
`data`) and returned others bare; errors were sometimes 200 with `success: false`, sometimes 400
for what was really 404, 403 or 422. The UI kept a hand-written `types.ts` that had drifted from
the services, hid the envelope mismatch with `.catch(() => undefined)` and could not tell a
validation error from an outage.

## Decision

Every endpoint returns the DTO itself, under `/api/v1`; every failure is an
`application/problem+json` (RFC 9457) with the status that says what happened - 404 for a
missing resource, 403 for a forbidden one, 409 for a conflict of state, 422 for anything the
request got wrong (model validation and domain rules alike), 401 for missing or bad credentials
and 423 for a locked account - carrying a `traceId` and, for validation, the messages per field.
Services throw domain exceptions; one handler maps them. Listings share one `PagedResponse<T>`.

The OpenAPI documents of the five services are exported from the built code into
`docs/api/openapi`, committed, and compared in CI; the UI's types are generated from them
(`openapi-typescript`) and nothing about the wire format is written by hand in the UI.

## Consequences

- A change of a response shape fails `tsc` in the UI and the `api:check` step in CI before it
  fails a page.
- The RTK Query endpoints are written by hand (a second generator would duplicate the types, and
  the operation names in the documents are generated); they are typed with the generated schemas.
- One error path in the UI: the base query turns a problem into an `ApiError`, one middleware
  turns it into one toast.
