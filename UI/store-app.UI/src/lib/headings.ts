/**
 * The level a component's heading takes in the page's outline. A card or a question is a level
 * below whatever it sits under: the page's h1 on its own page, a section's h2 on the home page.
 */
export type HeadingLevel = 2 | 3 | 4;

/** The element for a heading of that level. */
export const headingTag = (level: HeadingLevel) => `h${level}` as const;
