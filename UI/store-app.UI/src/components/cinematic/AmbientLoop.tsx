import { useEffect, useRef, useState } from 'react';
import { cn } from '@/lib/utils';
import { motionAllowed, savesData, screenMatches, WIDE_SCREEN_QUERY, type MotionFlag } from '@/lib/motion';

// The loop is fetched when its picture is this close to the screen
const NEAR_SCREEN = '200px 0px';

interface AmbientLoopProps {
  /** The loop's first (and last) frame: what everyone sees first, and all that phones and reduced motion see. */
  poster: string;
  /** A seamless loop in which only the light moves. */
  video: string;
  /** The switch in src/lib/motion.ts that turns this loop off. */
  flag: MotionFlag;
  className?: string;
  /** Plays on phones too (a light clip); by default only large screens fetch it. */
  phones?: boolean;
  /** The picture is the page's largest paint: load it first. */
  priority?: boolean;
  /** Plays once and stays on its last frame instead of looping. */
  once?: boolean;
}

/**
 * A decorative still picture that comes alive: once it nears the screen, a seamless loop of the
 * same frame (only the light moves) is fetched and fades in over it, and it pauses whenever it is
 * scrolled away. With the flag off, reduced motion, Data Saver or (unless `phones`) a small screen,
 * the picture alone. With `once`, a short clip instead: played to its end, it stays on its last frame.
 * Fills its box (object-cover): size and round the box from outside.
 */
const AmbientLoop = ({ poster, video, flag, className, phones = false, priority = false, once = false }: AmbientLoopProps) => {
  const [enabled] = useState(() => motionAllowed(flag) && !savesData() && (phones || screenMatches(WIDE_SCREEN_QUERY)));
  const [near, setNear] = useState(false);
  const [playing, setPlaying] = useState(false);
  const box = useRef<HTMLDivElement>(null);
  const clip = useRef<HTMLVideoElement>(null);
  // A clip played once is not started again when it comes back on screen
  const finished = useRef(false);

  useEffect(() => {
    const element = box.current;
    if (!enabled || !element || typeof IntersectionObserver === 'undefined') return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) setNear(true);
        const current = clip.current;
        if (!current) return;
        if (entry.isIntersecting && !finished.current) void current.play().catch(() => undefined);
        else current.pause();
      },
      { rootMargin: NEAR_SCREEN },
    );
    observer.observe(element);
    return () => observer.disconnect();
  }, [enabled]);

  return (
    <div ref={box} className={cn('relative overflow-hidden bg-muted', className)}>
      <img src={poster} alt="" fetchPriority={priority ? 'high' : undefined} className="absolute inset-0 h-full w-full object-cover" />
      {near && (
        <video
          ref={clip}
          src={video}
          muted
          loop={!once}
          autoPlay
          playsInline
          preload="auto"
          aria-hidden
          tabIndex={-1}
          onPlaying={() => setPlaying(true)}
          onEnded={() => (finished.current = true)}
          className={cn('absolute inset-0 h-full w-full object-cover opacity-0 transition-opacity duration-1000', playing && 'opacity-100')}
        />
      )}
    </div>
  );
};

export default AmbientLoop;
