# 018 - The admin panel hears of orders through a SignalR hub, fed by the order events

**Status**: accepted

## Context

The admin panel's order list and dashboard were read when opened: a new order, or one paid a
minute later, showed only after a reload. The order service already publishes every change -
`OrderPlaced` at checkout, `OrderStatusChanged` for each move - through its outbox, after the
change is committed. The browser reaches the services only through nginx and the gateway, carries
a 15-minute access token in memory (ADR 009) and no cookie the API would read, and may run on
several replicas of each service in Container Apps.

## Decision

- The order service maps a SignalR hub, `LiveOrdersHub`, at `/api/v1/admin/orders/live`, for the
  `Admin` policy (the demo administrator included). It only sends: `OrderChanged` with the order id,
  the status it entered and when. Nothing about the customer - the panel reads the order again
  through the API, where the demo administrator's masking applies.
- The hub is fed by a consumer of `OrderPlaced` and `OrderStatusChanged` in the same service,
  listening on a queue of its own instance (a temporary endpoint named with a random instance id,
  removed by the broker when the instance stops). Every instance hears every event, so a panel
  connected to any of them is told whichever instance made the change; the broker is the only
  backplane. The events come from the outbox, so the panel is never told of an order it cannot
  read yet.
- One transport: WebSockets without the negotiate request, so a connection never needs the same
  instance for two requests. The connection closes when its token expires; the panel reconnects
  with a fresh one (`freshAccessToken`), retrying for as long as it is open, and reads the orders
  again after a reconnect, having missed what happened meanwhile.
- A browser cannot put a header on a WebSocket request, so the SignalR client sends the token in
  the `access_token` query parameter. The gateway accepts it there only on the routes whose metadata
  say `AccessTokenInQuery` - this one - and moves it into the Authorization header before
  forwarding, so the service authenticates as on any request and the token leaves the forwarded
  address and the gateway's log. nginx logs that location without the query string and passes the
  upgrade on; Vite's proxy does the same in development.

## Consequences

- A new order shows on the list about a second after it is placed (the outbox's delivery), a paid
  one as soon as the webhook is taken in; the panel announces both.
- Each instance of the order service has a queue while it runs and handles every order event once
  more; at this shop's volume that is nothing. Another consumer that needs every instance to hear
  an event can use the same pattern.
- A token in a query string is the usual price of WebSockets in a browser. It is limited to one
  route, kept out of the logs we write, and lives 15 minutes; a proxy in front of the shop that logs
  full addresses would still see it.
