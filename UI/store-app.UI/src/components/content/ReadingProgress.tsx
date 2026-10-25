import { useEffect, useRef, type RefObject } from 'react';

/**
 * A hairline across the top of the screen that fills as the text is read: how far the reader is
 * through `target` (the article's body), not through the whole page with its products below.
 * Decorative - the scroll bar says the same to assistive technology.
 */
const ReadingProgress = ({ target }: { target: RefObject<HTMLElement | null> }) => {
  const bar = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let frame = 0;
    const update = () => {
      frame = 0;
      const body = target.current;
      if (!body || !bar.current) return;
      const rect = body.getBoundingClientRect();
      const read = (window.innerHeight - rect.top) / (rect.height || 1);
      bar.current.style.transform = `scaleX(${Math.min(1, Math.max(0, read)).toFixed(3)})`;
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
  }, [target]);

  return <div ref={bar} aria-hidden className="fixed inset-x-0 top-0 z-60 h-0.5 origin-left scale-x-0 bg-brand" />;
};

export default ReadingProgress;
