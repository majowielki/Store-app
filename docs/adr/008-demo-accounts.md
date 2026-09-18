# 008 - Demo accounts exist only where they are switched on

**Status**: accepted

## Context

The showcase needs a customer and an administrator anybody can sign in as, without a password.
The first version seeded them in every environment with passwords in the source code, and the
password-less endpoints were always there.

## Decision

`Demo:Enabled` (off by default) controls both the seeding of the two accounts and the
`demo-login` / `demo-admin-login` endpoints, which answer 404 when it is off. The accounts'
passwords are optional configuration; without one an account gets a random password nobody
knows, since the demo endpoints sign in without it. The gateway rate-limits the demo endpoints
like every other credential endpoint. The profile carries `isDemo`, and the identity service
refuses to change a demo account's address, so every visitor finds the shared profile as it was.

## Consequences

- A deployment that is not a showcase has no demo accounts at all.
- Everything a demo user does is real data in the store (orders, cart); the checkout keeps the
  shared profile read-only and the demo administrator may change nothing (007).
- The end-to-end tests use the demo accounts, so the stack they run against has the switch on.
