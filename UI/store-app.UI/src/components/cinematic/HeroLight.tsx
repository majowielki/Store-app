import { useEffect, useState } from 'react';
import { useDarkTheme } from '@/hooks/use-dark-theme';
import { cn } from '@/lib/utils';
import { motionAllowed, motionFlags, savesData, screenMatches, WIDE_SCREEN_QUERY } from '@/lib/motion';

// The first sign of a visitor (not a crawler or Lighthouse, which never scroll nor move a pointer)
const INTERACTIONS = ['scroll', 'pointermove', 'touchstart', 'keydown'] as const;
/** The loop runs slower than it was filmed: the light drifts, it never flickers past. */
const LIGHT_PLAYBACK_RATE = 0.7;

/** A picture and the seamless loop that starts and ends on it (only the light moves in it). */
export interface HeroScene {
  image: string;
  video: string;
  width: number;
  height: number;
}

interface HeroLightProps {
  /** The room in the morning: the light theme's scene. */
  morning: HeroScene;
  /** The same room at sunset, for the dark theme (switch: eveningHero). */
  evening?: HeroScene;
  alt: string;
  className?: string;
}

/**
 * The home page's hero picture with the light moving across it - the morning's in the light theme,
 * the sunset's in the dark one. The picture is the page's largest paint and stays one; the loop is
 * fetched only after the visitor's first scroll, pointer move, touch or key, on a large screen, with
 * motion and data not saved, and fades in over the picture once it plays. Its frames are cropped
 * like the picture's, so nothing jumps.
 */
const HeroLight = ({ morning, evening, alt, className }: HeroLightProps) => {
  const dark = useDarkTheme();
  const scene = dark && evening && motionFlags.eveningHero ? evening : morning;
  const [wanted, setWanted] = useState(false);
  const [playing, setPlaying] = useState<string | null>(null);

  useEffect(() => {
    if (!motionAllowed('heroLight') || savesData() || !screenMatches(WIDE_SCREEN_QUERY)) return;
    const start = () => setWanted(true);
    INTERACTIONS.forEach((type) => window.addEventListener(type, start, { once: true, passive: true }));
    return () => INTERACTIONS.forEach((type) => window.removeEventListener(type, start));
  }, []);

  return (
    <div className={cn('relative overflow-hidden bg-muted', className)}>
      <img
        key={scene.image}
        src={scene.image}
        width={scene.width}
        height={scene.height}
        alt={alt}
        fetchPriority="high"
        className="absolute inset-0 h-full w-full animate-zoom-out object-cover"
      />
      {wanted && (
        <video
          key={scene.video}
          src={scene.video}
          muted
          loop
          autoPlay
          playsInline
          preload="auto"
          aria-hidden
          tabIndex={-1}
          onLoadedMetadata={(event) => (event.currentTarget.playbackRate = LIGHT_PLAYBACK_RATE)}
          onPlaying={() => setPlaying(scene.video)}
          onError={() => setWanted(false)}
          className={cn('absolute inset-0 h-full w-full object-cover opacity-0 transition-opacity duration-1000', playing === scene.video && 'opacity-100')}
        />
      )}
    </div>
  );
};

export default HeroLight;
