import { useEffect, useRef } from 'react';

/**
 * Moves the element slightly against the scroll while it is on screen, by `strength` pixels
 * at most either way; the element should be larger than its frame (scaled) to hide the edges.
 * Nothing moves for visitors who asked for less motion.
 */
export const useParallax = <T extends HTMLElement>(strength = 40) => {
  const ref = useRef<T>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    let frame = 0;
    const update = () => {
      frame = 0;
      const rect = element.getBoundingClientRect();
      const viewport = window.innerHeight;
      if (rect.bottom < 0 || rect.top > viewport) return;
      // -1 when the element's centre is at the bottom edge of the screen, 1 at the top edge
      const position = (viewport / 2 - (rect.top + rect.height / 2)) / (viewport / 2 + rect.height / 2);
      element.style.transform = `translate3d(0, ${(position * strength).toFixed(1)}px, 0) scale(1.12)`;
    };
    const onScroll = () => {
      if (!frame) frame = requestAnimationFrame(update);
    };
    update();
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll);
    return () => {
      window.removeEventListener('scroll', onScroll);
      window.removeEventListener('resize', onScroll);
      if (frame) cancelAnimationFrame(frame);
    };
  }, [strength]);

  return ref;
};
