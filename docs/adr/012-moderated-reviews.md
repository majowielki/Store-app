# 012 - Reviews are published only after moderation

**Status**: accepted

## Context

Product reviews make the shop believable, and writing one after a purchase is a flow worth
showing. But the shop is a public demo: anyone can register and "buy" (nothing is charged), so a
verified purchase keeps nobody out, and the demo accounts are shared by every visitor. Whatever
a visitor types must not appear on the site unchecked.

## Decision

A review service with its own database. Reviews seeded with the catalogue are published. A new
review may be written only for a product the customer has paid for (the service learns purchases
from the order events), one per product, and passes automatic checks first: 20 to 1000
characters, no links, e-mail addresses or phone numbers, no words from an English and Polish
profanity list, at most three a day per account. It then waits for the true administrator, who
approves or rejects it with a reason; its author sees it meanwhile, marked as awaiting
moderation. A review written from a demo account is tied to that sign-in session and removed
after 24 hours. A reported review is hidden again until moderated. The service publishes each
product's rating summary as an event, and the product service keeps the average and the count
for listings and sorting.

## Consequences

- Nothing a visitor writes is public before a person has read it; the cost is a moderation queue
  for the true administrator (the demo administrator sees it and changes nothing, 007).
- The rating on a product card is eventually consistent with the reviews.
- A language model can later approve clearly harmless reviews on its own, behind a feature flag,
  without changing the flow for the rest.
