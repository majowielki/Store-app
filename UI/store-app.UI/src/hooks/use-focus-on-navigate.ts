import { useEffect, useRef, type RefObject } from 'react';
import { useLocation } from 'react-router-dom';

/**
 * Moves the focus to the page's content when the address turns to another page, as a browser does
 * on a full page load: a keyboard or screen reader user goes on from the top of the new page instead
 * of from the link they followed, which is gone or still in the header. Only the path counts:
 * filters, sorting and the pages of a list (the query string) leave the focus where it is.
 * The first page of the visit is left alone, the browser has placed the focus already.
 */
export const useFocusOnNavigate = (target: RefObject<HTMLElement | null>) => {
  const { pathname } = useLocation();
  const previous = useRef(pathname);

  useEffect(() => {
    if (previous.current === pathname) return;
    previous.current = pathname;
    // ScrollRestoration puts the page at its top (or where Back left it); the focus must not scroll it
    target.current?.focus({ preventScroll: true });
  }, [pathname, target]);
};
