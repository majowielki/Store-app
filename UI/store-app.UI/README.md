# Store UI

React 18 + TypeScript + Vite single-page app for the store. It talks to the API gateway under
`/api/v1` on its own origin: the Vite dev server proxies that path to the gateway (`vite.config.ts`)
and the container's nginx does the same (`docker/`), so the refresh cookie needs no CORS.

## Commands

| Command | What it does |
|---------|--------------|
| `npm run dev` | dev server on <http://localhost:5173>, API proxied to `GATEWAY_URL` (default `http://localhost:5000`) |
| `npm run build` | `tsc -b` (app, tests and tooling) and the production bundle in `dist/` |
| `npm run lint` | ESLint, warnings included (`--max-warnings=0` in CI) |
| `npm test` | Vitest: unit and component tests (`src/**/*.test.ts(x)`), API mocked with MSW |
| `npm run test:coverage` | the same with a coverage report (`coverage/`) |
| `npm run e2e` | Playwright scenarios in `e2e/` against a running stack (see below) |
| `npm run api:generate` | regenerates `src/api/schema/*.ts` from `docs/api/openapi/*.json` |
| `npm run api:check` | fails when the generated schema files are out of date (CI) |

## How the app is put together

- `src/api/` - the API layer. `api.ts` creates the one RTK Query instance; `auth.ts`, `cart.ts`,
  `catalog.ts`, `orders.ts`, `admin.ts` and `newsletter.ts` inject the endpoints of the
  corresponding service and export the hooks. `baseQuery.ts` sends every request: it attaches the
  bearer token, turns a problem response (`application/problem+json`) into an `ApiError`
  (`problem.ts`) and renews an expired access token once before giving up. `session.ts` keeps the
  access token in memory; the refresh token lives in an httpOnly cookie the page never sees.
  `errorToasts.ts` is the one place a failed request becomes a toast. `types.ts` aliases the
  generated schemas (`schema/`) - nothing about the wire format is written by hand.
- `src/features/session/` - who is signed in. `sessionThunks.ts` signs in (merging the guest cart
  once), restores the session after a page load and signs out; `roles.ts` reads the roles the
  profile carries.
- `src/features/cart/` - the cart. For a signed-in user the server cart is the source of truth
  (`useCart` reads the `getCart` query, every change is a mutation that answers with the whole
  cart); a visitor's cart lives in `guestCartSlice` and `localStorage` until sign-in.
  `pricing.ts` previews the totals the order service will compute.
- `src/routes/guards.ts` - route loaders that wait for the session check and redirect; the admin
  routes are loaded lazily (`App.tsx`), so the admin bundle (charts included) stays out of the shop.
- `src/pages/`, `src/components/` - the screens; `components/ui/` are shadcn/ui primitives.

## Tests

Unit and component tests run under jsdom with MSW answering the requests (`src/test/handlers.ts`,
fixtures typed with the generated contract in `src/test/fixtures.ts`). `src/test/render.tsx`
renders a component with a fresh store and router.

The Playwright scenarios need the whole stack: either `docker compose up` in the repository root
and `E2E_BASE_URL=http://localhost:8081 npm run e2e`, or the services started by hand (see the
repository README) with the dev server, which `npm run e2e` starts itself. The true administrator's
credentials come from `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD` (compose defaults otherwise), and
because every scenario signs in on its own, the gateway's sign-in limit has to be raised for a run
(`AUTH_CREDENTIAL_LIMIT` in compose, `--RateLimiting:Auth:CredentialPermitLimit=...` by hand).
