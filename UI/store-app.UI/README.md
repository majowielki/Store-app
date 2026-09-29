# Store UI

React 19 + TypeScript + Vite 8 single-page app for the store. It talks to the API gateway under
`/api/v1` on its own origin: the Vite dev server proxies that path to the gateway (`vite.config.ts`)
and the container's nginx does the same (`docker/`), so the refresh cookie needs no CORS. Node 22.12
or newer, which Vite 8 needs (`.nvmrc` names 22, CI and the `node:22-alpine` image take its latest release).

## Commands

| Command | What it does |
|---------|--------------|
| `npm run dev` | dev server on <http://localhost:5173>, API proxied to `GATEWAY_URL` (default `http://localhost:5000`) |
| `npm run build` | `tsc -b` (app, tests and tooling) and the production bundle in `dist/` |
| `npm run lint` | ESLint, warnings included (`--max-warnings=0` in CI) |
| `npm test` | Vitest: unit and component tests (`src/**/*.test.ts(x)`), API mocked with MSW |
| `npm run test:coverage` | the same with a coverage report (`coverage/`) |
| `npm run e2e` | Playwright scenarios in `e2e/` against a running stack (see below) |
| `npm run storybook` | Storybook on <http://localhost:6006>: the components with the API mocked by MSW |
| `npm run storybook:build` | the static Storybook in `storybook-static/` |
| `npm run test:visual:docker` | a screenshot of every story, compared with `visual/__screenshots__` (in the Playwright image; `-- --update-snapshots` after an intended change) |
| `npm run api:generate` | regenerates `src/api/schema/*.ts` from `docs/api/openapi/*.json` |
| `npm run api:check` | fails when the generated schema files are out of date (CI) |

## How the app is put together

- `src/api/` - the API layer. `api.ts` creates the one RTK Query instance; `auth.ts`, `cart.ts`,
  `catalog.ts`, `orders.ts` and `admin.ts` inject the endpoints of the corresponding service
  and export the hooks. `baseQuery.ts` sends every request: it attaches the
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
  `pricing.ts` previews the totals the way the order service computes them, with the rules it
  publishes (`GET /orders/pricing-rules`) - no price rule is written into the UI.
- `src/routes/guards.ts` - route loaders that wait for the session check and redirect; the admin
  routes are loaded lazily (`App.tsx`), so the admin bundle (charts included) stays out of the shop.
- `src/pages/`, `src/components/` - the screens; `components/ui/` are shadcn/ui primitives.
- `src/utils/` - formatting shared by every screen: `formatAsDollars` for amounts (USD) and
  `formatDate` / `formatDateTime` for dates, always in English whatever the browser's language.

## Look and feel

The shop is built from shadcn/ui primitives only, restyled through the theme rather than per
screen:

- **Colours** are the shadcn tokens in `src/index.css` - a warm light theme (linen and ink) and a
  dark one (warm charcoal), plus a `brand` token (terracotta) for sales, the cart count and
  highlighted words, and `success` for discounts. Tailwind 4 reads its theme from the same file: the
  `@theme` block of `src/index.css` maps the tokens to classes (there is no `tailwind.config.js`).
- **Type** is Fraunces (display headings, the `display` class) and Geist (everything else), both
  bundled from `@fontsource-variable`, so the content security policy needs no font host.
- **Motion** comes from a few pieces: `components/Reveal` fades sections in as they scroll into
  view, the keyframes in the `@theme` block (marquee, line reveal, float), `components/Marquee`
  and the view transition that carries a product image from a listing to the product page
  (`hooks/use-open-product`). Everything stops for visitors who ask for reduced motion.
- **Layout**: `pages/HomeLayout` puts the announcement bar, the header (with `DesktopNav`,
  `SearchDialog` on Ctrl+K and `MobileMenu`), the page and the footer together; phones get the
  floating `MobileBottomBar` instead of the header's account and cart buttons.

## Tests

Unit and component tests run under jsdom with MSW answering the requests (`src/test/handlers.ts`,
fixtures typed with the generated contract in `src/test/fixtures.ts`). `src/test/render.tsx`
renders a component with a fresh store and router.

The Playwright scenarios need the whole stack: either `docker compose up` in the repository root
and `E2E_BASE_URL=http://localhost:8081 npm run e2e`, or the services started by hand (see the
repository README) with the dev server, which `npm run e2e` starts itself. The true administrator's
credentials come from `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD` (compose defaults otherwise), and
because every scenario signs in on its own and reloads pages (each load renews the session), the
gateway's /auth limits have to be raised for a run (`AUTH_CREDENTIAL_LIMIT` and `AUTH_PERMIT_LIMIT`
in compose, `--RateLimiting:Auth:CredentialPermitLimit=...` and `--RateLimiting:Auth:PermitLimit=...`
by hand). The scenarios pay with the test cards through the real payment service and its
webhooks, and top up the stock of the products they pick through the true administrator's API,
since every run keeps the pieces it bought held (paid orders are not shipped).

`e2e/accessibility.spec.ts` scans the key pages with axe (`e2e/a11y.ts`, WCAG 2.2 AA; a serious
or critical finding fails) and makes a whole purchase with the keyboard alone; see ADR 017 for the
skip link and where the focus goes when the page changes.

## Storybook and the visual tests

`.storybook/` renders the components the way the app does: a fresh store and a router per story,
the theme from the toolbar, and the MSW handlers of `src/test/handlers.ts` answering the API in the
browser (a story adds its own under `parameters.msw.handlers.overrides`). The stories sit next to
their components (`*.stories.tsx`); `src/stories/data.ts` holds real catalogue pieces with their
pictures, which Storybook serves from `Blobs/w400` at `/pictures`.

`visual/stories.spec.ts` opens every story of the built Storybook and compares a screenshot of it
with the one committed in `visual/__screenshots__`, pixel for pixel. Fonts are drawn differently on
every system, so the screenshots are Linux ones: `npm run test:visual:docker` runs the tests in the
Playwright image of the installed version, as CI does after `npm run storybook:build`. A failed run
leaves the expected, actual and difference images in `test-results-visual/` (an artifact in CI);
after an intended change, or a Playwright upgrade, run it with `-- --update-snapshots` and commit
the new screenshots.

## The container

`Dockerfile` builds the bundle and serves it with nginx (`docker/`): `/api/` is proxied to
`GATEWAY_UPSTREAM`, `/config.js` is written at start from `API_BASE_URL`, and every page comes
with the security headers of `docker/security-headers.inc.template` (a content security policy
that allows scripts only from the bundle, images from any https host and the contact page's map).
When `API_BASE_URL` is absolute, `docker/15-csp.envsh` adds that origin to `connect-src`;
`CSP_CONNECT_SRC` overrides it. HSTS is for the ingress that terminates TLS.
