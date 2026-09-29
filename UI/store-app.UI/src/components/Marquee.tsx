import type { CSSProperties, ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface MarqueeProps {
  children: ReactNode;
  /** One full loop; longer is calmer. */
  duration?: string;
  reverse?: boolean;
  /** Stopped where it is, until played again (the visitor's pause button). */
  paused?: boolean;
  className?: string;
}

/**
 * An endless horizontal ticker: the content twice in a row, moved left by one copy's width.
 * The copy is hidden from assistive technology and from the tab order; hovering pauses it.
 * Each copy must be at least as wide as the screen, so pass enough content.
 */
const Marquee = ({ children, duration = '40s', reverse = false, paused = false, className }: MarqueeProps) => (
  <div className={cn('group flex overflow-hidden', className)}>
    <div
      className={cn('flex w-max shrink-0 animate-marquee group-hover:paused', paused && 'paused')}
      style={{ '--marquee-duration': duration, animationDirection: reverse ? 'reverse' : undefined } as CSSProperties}
    >
      <div className="flex shrink-0 items-center">{children}</div>
      <div className="flex shrink-0 items-center" aria-hidden inert>
        {children}
      </div>
    </div>
  </div>
);

export default Marquee;
