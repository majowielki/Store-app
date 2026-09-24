import { useEffect, useState } from 'react';
import { useInView } from '@/hooks/use-in-view';

/** A number counting up from zero the first time it scrolls into view. */
const CountUp = ({ value, duration = 1400 }: { value: number; duration?: number }) => {
  // Any part on screen is enough: a number waiting at the fold should not sit at zero
  const [ref, inView] = useInView<HTMLSpanElement>('0px');
  const [shown, setShown] = useState(0);

  useEffect(() => {
    if (!inView) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      setShown(value);
      return;
    }
    let frame = 0;
    const start = performance.now();
    const tick = (now: number) => {
      const progress = Math.min(1, (now - start) / duration);
      // Ease out: quick at first, settling on the value
      setShown(Math.round(value * (1 - Math.pow(1 - progress, 3))));
      if (progress < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [inView, value, duration]);

  return (
    <span ref={ref} className="tabular-nums">
      {shown}
    </span>
  );
};

export default CountUp;
