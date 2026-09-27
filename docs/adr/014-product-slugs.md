# 014 - Content names products by slug, not by id

**Status**: accepted (changes the product reference of 010)

## Context

010 had content refer to products by id. The content service seeds five lookbooks and three
collections that point at demo products, but a product's id depends on the database it was
created in: a fresh database numbers the demo products from 1, one seeded by an older catalogue
has them further on, and nothing lets the content seed learn the ids without calling the catalogue
while it migrates - which no service does (a service waits only for its database and the broker).

## Decision

Every product gets a slug: lowercase words joined by dashes, made from the title when the product
is created ("Bouclé Modular Sofa" -> `boucle-modular-sofa`, a clash gets "-2"), unique, and kept
when the title changes. Products created before slugs were introduced got one from their title in
the same migration. Content stores slugs; the shop asks the catalogue for them in one request,
`GET /products?slugs=a,b,c`, and leaves out the ones it no longer lists. The demo data of both
services is checked against each other by a unit test.

## Consequences

- Seeds of different services can name the same product without knowing each other's ids.
- A product keeps its slug for life. Renaming it does not change its address, so content never
  points at nothing because of a new title; the admin panel shows the slug, it cannot edit it.
- The slug is ready to serve as the product's public address when product pages move to one.
- The points on a product's main picture (the other products the room shows) follow the same
  rule inside the catalogue: a point stores the slug, so the seed can name a product of a later
  photo round before it exists, and the point appears once that product is added.
