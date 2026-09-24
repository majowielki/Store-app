import { useEffect, useRef, useState } from 'react';

/**
 * Whether the element has been scrolled into view; once true it stays true, so what came in
 * does not animate out again. True at once where IntersectionObserver is missing (tests, old browsers).
 */
export const useInView = <T extends Element>(rootMargin = '0px 0px -8% 0px') => {
  const ref = useRef<T>(null);
  const [inView, setInView] = useState(false);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;
    if (typeof IntersectionObserver === 'undefined') {
      setInView(true);
      return;
    }
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setInView(true);
          observer.disconnect();
        }
      },
      { rootMargin },
    );
    observer.observe(element);
    return () => observer.disconnect();
  }, [rootMargin]);

  return [ref, inView] as const;
};
