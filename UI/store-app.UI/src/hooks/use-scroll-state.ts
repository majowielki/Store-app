import { useEffect, useState } from 'react';

interface ScrollState {
  /** The page has left its very top: the header gets its blurred background. */
  scrolled: boolean;
  /** Scrolling down past the first screen: the header slides away until the visitor scrolls back up. */
  hidden: boolean;
}

export const useScrollState = (): ScrollState => {
  const [state, setState] = useState<ScrollState>({ scrolled: false, hidden: false });

  useEffect(() => {
    let lastY = window.scrollY;
    let frame = 0;
    const update = () => {
      frame = 0;
      const y = window.scrollY;
      const delta = y - lastY;
      lastY = y;
      setState((previous) => {
        const scrolled = y > 8;
        // A few pixels of either way are jitter (trackpads, the mobile toolbar), not a direction
        const hidden = y < 200 ? false : delta > 4 ? true : delta < -4 ? false : previous.hidden;
        return scrolled === previous.scrolled && hidden === previous.hidden ? previous : { scrolled, hidden };
      });
    };
    const onScroll = () => {
      if (!frame) frame = window.requestAnimationFrame(update);
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => {
      window.removeEventListener('scroll', onScroll);
      if (frame) window.cancelAnimationFrame(frame);
    };
  }, []);

  return state;
};
