# 009 - Access token in memory, refresh token in an httpOnly cookie

**Status**: accepted

## Context

The first UI kept a one-hour access token in `localStorage`, where any script that ran on the
page could read it, and had no way to end a session on the server or to renew it without
signing in again.

## Decision

The identity service issues a 15-minute access token and a refresh token: 256 random bits,
stored hashed with a family id, sent to the browser only as a cookie (`HttpOnly`, `Secure`,
`SameSite=Strict`, path `/api/v1/auth`). `POST /auth/refresh` rotates it in one conditional
update, so two tabs refreshing at once rotate it once; a spent token presented again revokes the
whole family. `POST /auth/logout` revokes the family and removes the cookie.

The UI keeps the access token in memory only. A page load trades the cookie for a new token and
reads `/auth/me`; route guards wait for that check, so who may open `/admin` is decided by the
profile the API returned. A 401 on any request triggers one shared refresh and one retry; a
refused refresh ends the session for the whole app. A flag in `localStorage` only remembers
whether a session was started in this browser, so a visitor who never signed in is not sent to
be refused.

## Consequences

- A script injected into the page finds no token to steal; a stolen refresh token is good until
  its first use by either party, after which both copies are dead.
- The UI must be served from the API's origin (the nginx proxy) for the cookie to travel; a UI on
  another origin needs CORS with credentials and a `SameSite` the cookie does not have.
- The refresh endpoint is not under the sign-in rate limit: the UI calls it on every page load.
