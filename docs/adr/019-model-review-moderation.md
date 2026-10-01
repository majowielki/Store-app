# 019 - A model reads new reviews before the administrator, when it is switched on

**Status**: accepted; amends [012](012-moderated-reviews.md)

## Context

Every review a customer writes waits for the true administrator (ADR 012), who in a demo shop may
not look for days, so a customer who wrote one rarely sees it published. Most reviews are plain
opinions about a product and need no person; the automatic checks already refuse links, contact
details and swearing, but not insults in other words, off-topic text or advertising.

## Decision

- Behind a feature flag (`ReviewModel:Enabled`) and only with an Anthropic API key
  (`ReviewModel:ApiKey`), a small model - Claude Haiku 4.5 by default (`ReviewModel:Model`) - reads
  each new review first. Without the flag or the key nothing changes: every review waits for the
  administrator.
- A review that passed the automatic checks is saved as `Pending` and, in the same transaction, a
  `ReviewAwaitsModel` message goes through the outbox to the service itself. Its consumer sends the
  model the stars, the title and the text - nothing about the author - as JSON, so nothing the
  customer wrote can pass for the instructions, and asks for a structured answer: a verdict,
  `clean` or `doubtful`, and one sentence why.
- `clean` publishes the review (no moderator; the audit records `REVIEW_PUBLISHED_BY_MODEL` with the
  model) and updates the product's rating. `doubtful` leaves it waiting with the model's reason
  shown to the administrator (`REVIEW_HELD_BY_MODEL`). A refusal, an answer cut short or one that
  is not the JSON asked for counts as doubtful. A model that cannot be reached after the client's
  retries gives no verdict, and the review waits as before.
- The instructions tell the model that criticism is clean: a review is never held because it is
  unfavourable to the shop. Doubtful are insults, threats, hate, sexual content, personal data,
  advertising, off-topic text, instructions addressed to the model and a rating the text plainly
  contradicts - or anything it is unsure of.
- The administrator still decides everything the model held and may reject what it published;
  the demo administrator sees the verdict, and the reason only where the text is shown.

## Consequences

- A clean review appears within seconds of being written; the queue holds only what a person
  should read, each with the model's reason.
- A request per review costs a fraction of a cent with Haiku; the consumer reads two reviews at a
  time per instance (`ReviewModel:ConcurrentReviews`), each holding a database connection while
  the model answers (up to `TimeoutSeconds`, with `MaxRetries`).
- The model can be wrong both ways. A clean verdict on a bad review is undone by a report or by the
  administrator; a doubtful one on a good review only delays it. Swapping the model is a
  configuration change; the tests play it with a scripted model and a fake Messages API.
