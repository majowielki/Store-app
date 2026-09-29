# 020 - The purchase funnel counts steps the pages send and the orders the events bring

**Status**: accepted

## Context

The dashboard showed orders and revenue, but not how many visitors looked at a product or put it
in the bag before the few who ordered. Visitors browse without an account and keep their bag in
the browser, so no service sees those steps; the orders, on the other hand, already reach the audit
service as `OrderPlaced` events. The shop sets no tracking cookies and should not start.

## Decision

- The product page and the quick view count a view, and every addition to the bag counts a step:
  the UI posts `{ kind, productId }` to `/api/v1/shop-events`. The gateway routes only that `POST`
  without a sign-in, under the per-client `api` rate limit. Nothing about the visitor is kept - no
  account, no address, no identifier - only the step, the product and the time.
- The audit service stores them in a table of their own (`ShopEvents`); the audit trail still takes
  entries only from events. The retention job deletes both after the same period.
- `GET /api/v1/auditlog/funnel?days=30` (administrators) counts the views, the bag additions and
  the `ORDER_PLACED` entries from midnight UTC that many days ago - the start of the order
  statistics - so the last stage is the number of orders the dashboard shows next to it. The window
  never reaches past the retention period.
- The dashboard draws the three stages as bars from one baseline, each as long as its share of the
  views, with the counts and the share of the stage before in words.

## Consequences

- The funnel counts steps, not people: a visitor who opens a product twice counts twice, and the
  shares are ratios of counts. It needs no consent banner and holds nothing personal.
- Anyone can send steps; the rate limit bounds how many, and the numbers are for the shop's own
  view of its catalogue, not for billing.
- A count lost in transit (a closed tab, a network error) is simply missing; the orders stage comes
  from the outbox and is exact.
