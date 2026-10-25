import { useEffect, useRef, useState, type ReactNode } from 'react';
import { markSeen, wasSeen } from '@/lib/filmSeen';
import { cn } from '@/lib/utils';
import { motionAllowed, savesData, screenMatches, WIDE_SCREEN_QUERY } from '@/lib/motion';

export interface ScrollFilmSource {
  /** The clip: its last frame is the picture it covers, cropped the same. */
  video: string;
  /** Its first frame, shown until the clip can be drawn. */
  poster: string;
}

// The clip is fetched once the picture is this close below the screen
const PRELOAD_MARGIN = '50% 0px';
// Under this progress the clip still covers the picture; at it the camera has arrived
const ARRIVED = 0.995;
// The walk starts once the picture's top has risen to this share of the screen's height, so its
// first steps (the doorway) are seen, not spent below the fold
const START_AT = 0.8;

const clamp01 = (value: number) => Math.min(1, Math.max(0, value));

/**
 * A picture that the visitor walks into by scrolling: over it, a clip whose time follows the
 * scroll - its start when the picture's top is four fifths down the screen, its last frame (the
 * picture itself) when the picture's middle reaches the screen's middle. Scrolling back walks out
 * again. The picture underneath is the real one, with everything it does (lookbook points): once
 * arrived, the clip fades away and leaves it. Keyboard focus inside shows the picture at once.
 * Large screens only, with motion and data not saved; elsewhere the picture alone.
 *
 * A picture at the top of its page (`trigger="arrival"`) is already on screen, where a scroll would
 * start the walk halfway: there the clip plays once by itself on arrival, then leaves the picture -
 * once per browser tab, like the sign-in film.
 */
const ScrollFilm = ({ film, trigger = 'scroll', className, children }: { film?: ScrollFilmSource; trigger?: 'scroll' | 'arrival'; className?: string; children: ReactNode }) => {
  const [enabled] = useState(
    () => !!film && motionAllowed('lookbookEntrance') && screenMatches(WIDE_SCREEN_QUERY) && !savesData() && !(trigger === 'arrival' && wasSeen(film.video)),
  );
  const [near, setNear] = useState(false);
  const [arrived, setArrived] = useState(false);
  const [focused, setFocused] = useState(false);
  const [failed, setFailed] = useState(false);
  const box = useRef<HTMLDivElement>(null);
  const video = useRef<HTMLVideoElement>(null);

  // Fetch the clip only when the picture is about to come into view
  useEffect(() => {
    const element = box.current;
    if (!enabled || !element || typeof IntersectionObserver === 'undefined') return;
    const observer = new IntersectionObserver(([entry]) => entry.isIntersecting && setNear(true), { rootMargin: PRELOAD_MARGIN });
    observer.observe(element);
    return () => observer.disconnect();
  }, [enabled]);

  // On arrival the clip plays by itself; if the browser refuses to (battery saver), the picture at once
  useEffect(() => {
    if (!enabled || !near || trigger !== 'arrival') return;
    video.current?.play().catch(() => setArrived(true));
  }, [enabled, near, trigger]);

  // The clip's time follows the scroll; a seek waits for the previous one, so a fast flick never queues up
  useEffect(() => {
    const element = box.current;
    if (!enabled || !element || trigger !== 'scroll') return;
    let frame = 0;
    let wanted: number | null = null;
    const seek = () => {
      const clip = video.current;
      if (wanted === null || !clip || clip.seeking || !clip.duration) return;
      clip.currentTime = wanted * (clip.duration - 0.05);
      wanted = null;
    };
    const update = () => {
      frame = 0;
      const rect = element.getBoundingClientRect();
      const viewport = window.innerHeight;
      // From the picture's top at START_AT of the screen to its middle at the screen's middle
      const start = viewport * START_AT;
      const end = viewport / 2 - rect.height / 2;
      const progress = clamp01((start - rect.top) / Math.max(1, start - end));
      setArrived(progress >= ARRIVED);
      wanted = progress;
      seek();
    };
    const onScroll = () => {
      if (!frame) frame = requestAnimationFrame(update);
    };
    const clip = video.current;
    update();
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll);
    clip?.addEventListener('seeked', seek);
    clip?.addEventListener('loadedmetadata', update);
    return () => {
      window.removeEventListener('scroll', onScroll);
      window.removeEventListener('resize', onScroll);
      clip?.removeEventListener('seeked', seek);
      clip?.removeEventListener('loadedmetadata', update);
      if (frame) cancelAnimationFrame(frame);
    };
  }, [enabled, near, trigger]);

  const covering = enabled && !failed && !arrived && !focused;

  return (
    <div ref={box} className={cn('relative', className)} onFocus={() => setFocused(true)}>
      {children}
      {enabled && film && !failed && (
        <div
          aria-hidden
          className={cn(
            'pointer-events-none absolute inset-0 overflow-hidden rounded-4xl bg-muted transition-opacity duration-500',
            covering ? 'pointer-events-auto opacity-100' : 'opacity-0',
          )}
        >
          <img src={film.poster} alt="" loading="lazy" className="absolute inset-0 h-full w-full object-cover" />
          {near && (
            <video
              ref={video}
              src={film.video}
              muted
              playsInline
              preload="auto"
              autoPlay={trigger === 'arrival'}
              tabIndex={-1}
              onEnded={() => {
                if (trigger !== 'arrival' || !film) return;
                markSeen(film.video);
                setArrived(true);
              }}
              onError={() => setFailed(true)}
              className="absolute inset-0 h-full w-full object-cover"
            />
          )}
        </div>
      )}
    </div>
  );
};

export default ScrollFilm;
