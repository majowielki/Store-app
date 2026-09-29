# Reviews

Product reviews and their moderation (`Services/ReviewService`, database `store_review_db`,
[ADR 012](../adr/012-moderated-reviews.md)). Nothing a visitor writes is shown before the true
administrator has read it.

## The moderation queue

The admin panel's **Reviews** page lists the queue: reviews waiting for moderation and published
ones a customer has reported. The true administrator approves or rejects them, one by one or
several at once, a rejection with the reason its author will see; every decision is an audit event
with the moderator. The demo administrator sees the queue with the unread texts and the accounts
masked and can change nothing.

- A new review waits as `Pending`; its author sees it on the product page, marked as awaiting
  moderation. It passed the automatic checks first: a paid order of the product, one review per
  product, 20 to 1000 characters, no links, e-mail addresses, phone numbers or words from the
  profanity list, at most three a day per account.
- A report from a customer's account takes a published review off the page until it is moderated
  again: approving it clears the report, rejecting it removes it from the page for good.
- A report from a demo account hides the review for that sign-in session only, for a day.

## The ratings on the product cards

Every change of what is published sends `ReviewSummaryChanged` with the product's average and
count; the catalogue keeps them for the cards and for sorting by rating. When the two disagree the
catalogue has missed an event: replay it (see [dead-letter-queue.md](dead-letter-queue.md)). The
event carries the totals, not a difference, so the next change of what the product shows (a review
approved, rejected or reported) also puts it right.

## The seeded reviews

`--migrate` seeds three to six published reviews per demo product, naming the products by slug
because their ids differ between databases. The service gives them their product ids by asking
the catalogue (`SeedReviewLinker`): every half a minute while some wait, then every ten minutes.
Reviews of a product the catalogue does not list (a retired one) keep waiting and are shown nowhere.

## The demo accounts' sandbox

A review or a report from a demo account belongs to that sign-in session (the token's
`session_id`) and is deleted a day later (`DemoSandboxCleanup`, every ten minutes); a deleted
review that was published takes its stars out of the product's rating.

## Restoring

The purchases a customer may review come from `OrderPaid`. After restoring `store_review_db` to a
point in time (see [database-restore.md](database-restore.md)), orders paid since then cannot be
reviewed until their events are replayed; the catalogue's ratings are published again as reviews
are moderated.
