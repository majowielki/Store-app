# The content service

Makers, collections, journal articles and lookbooks: the editorial pages of the shop
(`Services/ContentService`, database `store_content_db`, [ADR 010](../adr/010-content-service.md)).

## What it depends on

Only its database and the broker, like every service. It does not call the catalogue: content
names products by slug ([ADR 014](../adr/014-product-slugs.md)) and the shop asks the catalogue
for them. A slug that no longer matches an active product is simply left out of the page, so a
deleted or renamed-away product never breaks a collection or a lookbook.

## Editing

The true administrator edits everything in the admin panel; the demo administrator can open the
lists and nothing else. Every change is an audit event (`COLLECTION_UPDATED`, `LOOKBOOK_DELETED`,
...), so the audit log shows who changed what and the previous values. Unpublishing hides an entry
from the shop and keeps it; a delete removes the row.

## Caching

The public reads carry a weak ETag (latest change time plus the number of entries) and
`Cache-Control: no-cache`: browsers keep the answer and revalidate it, and an unchanged page costs
a 304 without a body. There is nothing to purge after an edit - the next request sees the new tag.

## The demo content

`--migrate` (the `content-migrate` container, the migration job in Azure) applies the migrations
and adds the demo content the database does not have yet, matched by slug. It never overwrites an
entry that exists: edited demo content stays edited. To get a demo entry back as it was, delete it
in the admin panel and run the migration job again.

The pictures live with the product pictures (`Blobs/`, the `product-images` container); a new
cover goes through `Scripts/optimize-images.cs` and the blob upload like any product picture.

## Restoring

Content has no outbox traffic worth replaying and nothing references it from other services, so
restoring `store_content_db` to a point in time (see [database-restore.md](database-restore.md))
only brings the pages back to that moment; the audit log keeps what was changed since.
