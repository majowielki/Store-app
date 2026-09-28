# 009 - Access token in memory, refresh token in an httpOnly cookie

**Status**: accepted

## Context

The first UI kept a one-hour access token in `localStorage`, where any script that ran on the
page could read it, and had no way to end a session on the server or to renew it without
signing in again.

## Decision

The identity service issues a 15-minute access token and a refresh token: 256 random bits,
stored hashed with a family id, sent to the browser only as a cookie (`HttpOnly`, `Secure`,
`SameSite=Strict`, path `/api/v1/auth`). `POST /auth/refresh` rotates it: spending the token is
one conditional update in the transaction that stores its successor, so two tabs refreshing at
once rotate it once. A spent token presented again revokes the whole family - with one exception,
the "reuse interval" of Auth0 and Okta: within `JwtSettings:RefreshTokenReuseSeconds` (30 s) of
its rotation, while the successor it got has not been used, the token is taken for a client that
never received the answer (a page left while its refresh was on the way, several tabs starting
together). That unseen successor is spent and a new one issued in the same family, and every token
that led to the spent successor now leads to the new one, so the race repeated inside the window
resolves the same way. `POST /auth/logout` revokes the family and removes the cookie.

The window was added after the end-to-end tests signed users out: every page load renews the
session, and a navigation that aborts the renewal leaves the browser with the spent token.

The UI keeps the access token in memory only. A page load trades the cookie for a new token and
reads `/auth/me`; route guards wait for that check, so who may open `/admin` is decided by the
profile the API returned. A 401 on any request triggers one shared refresh and one retry; a
refused refresh ends the session for the whole app. A flag in `localStorage` only remembers
whether a session was started in this browser, so a visitor who never signed in is not sent to
be refused.

## Consequences

- A script injected into the page finds no token to steal; a stolen refresh token is good until
  its first use by either party, after which both copies are dead. The reuse window lets a copy
  used within 30 s of the rotation, before the owner used the successor, into the session; the
  owner's next refresh of a token spent that way, once the window has passed, ends the session for
  both.
- Answers applied out of order by the browser (a later cookie overwritten by an earlier one) are
  covered only within the window; outside it such a client signs in again.
- The UI must be served from the API's origin (the nginx proxy) for the cookie to travel; a UI on
  another origin needs CORS with credentials and a `SameSite` the cookie does not have.
- The refresh endpoint is not under the sign-in rate limit: the UI calls it on every page load.
