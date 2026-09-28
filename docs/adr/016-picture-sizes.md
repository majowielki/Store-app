# 016 - Every picture has smaller copies next to it, found by name

**Status**: accepted

## Context

The shop's pictures are WebP files in one blob container, 1600 px wide for product shots and
2000 px for the editorial ones, about 100 kB each. The catalogue, the content service, carts and
orders store their full addresses. A product card shows its picture about 300 px wide and a cart
line about 80 px, yet every page loaded the full file: twelve cards were over a megabyte. The
pictures change rarely, are generated on a developer's machine, and are committed so that a fresh
clone shows them.

## Decision

- `Scripts/make-image-sizes.cs` makes, for every `Blobs/Name.webp`, the copies
  `w400/Name.webp`, `w800/Name.webp` and `w1200/Name.webp`, and a 32 px `w32/Name.webp`. They are
  committed with the pictures and uploaded with them (`blobs-seed` in development,
  `Scripts/Upload-Blobs.ps1` to Azure), so the container holds each copy next to its picture.
- Nothing stores the copies' addresses. The UI derives them from the picture's
  (`src/lib/images.ts`): an absolute address of a WebP file gets a `srcset` of the three copies
  and the picture itself, and the `sizes` of the place it is shown in (a card, a thumbnail, half
  or all of the page), so the browser loads the smallest copy that fills it. `ResponsiveImage`
  draws the 32 px copy behind the large pictures until they arrive.
- A picture from elsewhere (an address an administrator typed, any other format) is shown as it
  is; should its copies be missing all the same, the picture falls back to its own address after
  the first failed load.

## Consequences

- A card loads a 10 kB copy instead of a 100 kB picture; phones with dense screens take the
  800 or 1200 px one. The copies add about 23 MB to the repository.
- A new or regenerated picture needs the script run again before it is committed; the widths are
  written in the script and in `src/lib/images.ts` and must stay the same.
- The API and the stored data did not change, and pictures kept in carts and orders get the copies
  too, since the rule depends only on the address.
