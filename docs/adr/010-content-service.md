# 010 - The shop's content lives in its own service

**Status**: accepted; products are referred to by slug since [014](014-product-slugs.md)

## Context

The shop is growing pages that are not the catalogue: makers (the five companies behind the
products), curated collections, journal articles and lookbooks - room photographs with points
that lead to products. They change more often than code and should be edited by the
administrator, not deployed. The options were files in the UI repository, tables in the
product service, or a service of their own.

## Decision

A content service with its own database (`content`), built like every other service (001): makers,
collections, articles and lookbooks (an image and points at percentage coordinates, each with a
product id). Public endpoints read what is published, with an ETag so browsers and the gateway
can cache it; the administrator's endpoints write, and the demo administrator reads only (007).
Content refers to products by id and nothing else; the UI asks the catalogue for those products
and leaves out the ones it no longer lists. Article and maker texts are Markdown, rendered by
the UI without raw HTML. Help and legal pages stay static in the UI - they change with the code.

## Consequences

- One more service to run, migrate, deploy and monitor, the same way as the others.
- A deactivated product can still be named by a collection or a lookbook point; the shop skips it
  and the admin panel marks it, rather than the content service listening to the catalogue.
- No join across services: a page with content and products makes two requests, both cacheable.
