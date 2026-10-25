/**
 * The names of the elements that travel from one page to the next in a view transition (styles:
 * [data-vt] in index.css). An element is named on click, just before the router navigates.
 */
export const VT = {
  /** A product tile's picture becoming the product page's picture. */
  productImage: 'product-image',
  /** An editorial card's cover (collection, look, maker, article) becoming the cover of its page. */
  contentCover: 'content-cover',
} as const;

export type ViewTransitionName = (typeof VT)[keyof typeof VT];

/**
 * Gives `element` the transition name, taking it from whichever element still carries it from an
 * earlier click: a name may stand on one element of the page only.
 */
export const nameForTransition = (element: Element | null | undefined, name: ViewTransitionName) => {
  document.querySelectorAll(`[data-vt="${name}"]`).forEach((other) => other.removeAttribute('data-vt'));
  element?.setAttribute('data-vt', name);
};
