# 015 - Search engines read the SPA, and each service lists its own pages

**Status**: accepted

## Context

The shop is a single-page app: nginx answers every address with the same `index.html`, and the
title, the description and the content of a page exist only once the app has run. Google renders
such pages; other crawlers and the social sites that build a card from a shared link do not. A
sitemap has to list every product and editorial page with its absolute address, but no single
part of the system knows them all: the catalogue owns the products, the content service the
makers, collections, articles and lookbooks, and only the UI knows its routes. The shop's public
address differs per deployment (a port on localhost, the Container Apps domain).

## Decision

- **Pages describe themselves in the browser.** Each page renders `PageMeta` (`src/seo`): the
  title, the description, the canonical address (the path without the query), the Open Graph
  card and `noindex` for the cart, the checkout, the account, search results and the admin panel.
  Products, articles, and the trail above a page carry schema.org data as JSON-LD. `index.html`
  keeps the shop's own title, description and card for the crawlers that do not run the app.
  No server-side rendering or prerendering.
- **Each service lists the pages of its data.** `GET /api/v1/products/sitemap.xml` and
  `GET /api/v1/content/sitemap.xml` answer with a sitemap of their published entries; the format
  and the addresses come from `Store.BuildingBlocks.Shop` (`Sitemap`, `ShopLinks`), so the routes
  of the UI are written once on the server side. The UI keeps a static sitemap of its own pages.
- **The UI container serves the index.** nginx serves `robots.txt`, `/sitemap.xml` (an index of
  the three sitemaps) and `/sitemap-pages.xml` from the build, and proxies `/sitemap-products.xml`
  and `/sitemap-content.xml` to the services, so the addresses search engines see do not depend on
  the API's version and `/api/` can stay out of `robots.txt`.
- **One public address, `SHOP_URL`.** The deployment gives the shop's address once: to the
  services as `Shop:Url` (sitemaps, and the links in e-mails, which used `Mail:ShopUrl` before),
  and to the UI container, whose nginx writes it into `robots.txt`, the sitemap index and the
  card picture of `index.html`, and whose `/config.js` gives it to the app for canonical links.

## Consequences

- Google indexes every product and editorial page with its title, description, price, stock and
  rating; the other crawlers see the shop's general description. Prerendering would change that,
  at the cost of a build step that knows the catalogue.
- A new public route of the UI is added to `public/sitemap-pages.xml` by hand; a new kind of
  content is added to the content service's sitemap.
- Without `SHOP_URL` the stack still starts; `robots.txt`, the sitemap index and the card picture
  then carry relative addresses, which search engines may ignore.
