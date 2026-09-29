/** The id of every layout's <main>: where the skip link leads and where the focus goes on a new page. */
export const MAIN_CONTENT_ID = 'main-content';

/**
 * The onCloseAutoFocus of a dialog, sheet or menu. Once one closes, Radix sends the focus back to the
 * button that opened it; but when a link inside it led to another page, the focus already stands on
 * that page's content (useFocusOnNavigate) and stays there instead of jumping back to the header.
 */
export const keepFocusOnNewPage = (event: Event) => {
  const main = document.getElementById(MAIN_CONTENT_ID);
  if (main?.contains(document.activeElement)) event.preventDefault();
};

/** keepFocusOnNewPage first; the component's own handler only when the focus is still to be placed. */
export const composeCloseAutoFocus =
  (own?: (event: Event) => void) =>
  (event: Event): void => {
    keepFocusOnNewPage(event);
    if (!event.defaultPrevented) own?.(event);
  };
