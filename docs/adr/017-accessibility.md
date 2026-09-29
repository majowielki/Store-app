# 017 - The shop keeps to WCAG 2.2 AA, checked by axe in the end-to-end tests

**Status**: accepted

## Context

The shop is a single-page application: a link replaces the page without a page load, so the
browser neither moves the focus nor tells a screen reader that the page changed. After a click in
the header, a keyboard user went on from the header; after a link inside the search or the bag,
Radix sent the focus back to the button that had opened it. Nothing let a keyboard user skip the
announcement bar and the header, the ticker in the bar moved with no way to stop it, and
Lighthouse and axe found text too pale on the terracotta tiles, headings skipping levels (page
titles were h2, the footer's columns h4) and controls whose spoken name did not contain the words
they show ("Amount" on a button reading "1").

## Decision

- The bar is WCAG 2.2 level AA. The end-to-end tests scan eight key pages (home, catalogue,
  product, article, About, sign-in, bag, checkout) and the payment steps with
  `@axe-core/playwright` (`e2e/a11y.ts`); a finding axe rates serious or critical fails the run.
  The rule that a control's name must contain its visible words is experimental in axe and is
  switched on.
- Each layout's `<main id="main-content" tabindex="-1">` is where the skip link, the first stop of
  the Tab key, leads. `useFocusOnNavigate` moves the focus to it when the path changes; a change of
  the query string alone (filters, sorting, the pages of a list) leaves the focus where it is.
- Sheets and dropdown menus (`components/ui`) and the quick view keep the focus on the new page
  when a link inside them led there (`keepFocusOnNewPage`); otherwise Radix returns it to the
  button that opened them, as before.
- One end-to-end scenario makes the whole purchase with the keyboard alone: sign-in, the search
  (Ctrl+K), the product, the bag, the checkout and the card.
- Moving content can be paused (the announcement bar has a pause button) and stands still for
  visitors who ask for less motion. Decorative text (the footer's wordmark) is drawn by CSS, so it
  is neither read out nor held to the contrast ratio; real text on coloured tiles is never faded
  with opacity.

## Consequences

- A change that breaks the bar on one of the scanned pages fails the end-to-end run, with the rule,
  its weight and the elements named. Moderate and minor findings are not enforced; the pages have
  none today, but only a later review will notice new ones.
- Pages outside the scanned set (the admin panel above all) are held to the same bar only by
  review; they share the layouts, the primitives and the components that the scanned pages use.
- Component headings take their level from where they are used (`headingLevel` on the content
  cards and the accordion), so a new page must pass the right one.
