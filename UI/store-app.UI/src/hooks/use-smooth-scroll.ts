import { useEffect } from 'react';
import type Lenis from 'lenis';
import { FINE_POINTER_QUERY, motionAllowed, screenMatches } from '@/lib/motion';

/** How long one turn of the wheel glides, in seconds (Lenis' own default is 1.2). */
const GLIDE_SECONDS = 1.1;

// Dialogs, sheets and menus scroll on their own, and the page behind them must stay put
const SCROLLS_ON_ITS_OWN = '[role="dialog"], [role="menu"], [role="listbox"], [data-radix-popper-content-wrapper]';
/** The attribute Radix' scroll lock (react-remove-scroll) puts on <body> while a dialog is open. */
const SCROLL_LOCKED = 'data-scroll-locked';

/**
 * Smooth scrolling of the shop's pages (Lenis) for mouse and trackpad users who did not ask for less
 * motion; touch screens keep their native scrolling. The library is fetched after the page is up,
 * so it never delays the first paint.
 */
export const useSmoothScroll = () => {
  useEffect(() => {
    if (!motionAllowed('smoothScroll') || !screenMatches(FINE_POINTER_QUERY)) return;
    let lenis: Lenis | undefined;
    let cancelled = false;
    // An open dialog locks the page (Radix marks <body>); Lenis would scroll it anyway, so it pauses
    const followLock = () => (document.body.hasAttribute(SCROLL_LOCKED) ? lenis?.stop() : lenis?.start());
    const lock = new MutationObserver(followLock);
    void import('lenis').then(({ default: Smooth }) => {
      if (cancelled) return;
      lenis = new Smooth({
        autoRaf: true,
        duration: GLIDE_SECONDS,
        allowNestedScroll: true,
        prevent: (node) => node.matches(SCROLLS_ON_ITS_OWN),
      });
      followLock();
      lock.observe(document.body, { attributes: true, attributeFilter: [SCROLL_LOCKED] });
    });
    return () => {
      cancelled = true;
      lock.disconnect();
      lenis?.destroy();
    };
  }, []);
};
