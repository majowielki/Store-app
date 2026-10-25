import { useEffect, useRef, useState } from 'react';
import { markSeen, wasSeen } from '@/lib/filmSeen';
import { cn } from '@/lib/utils';
import { motionAllowed, savesData, screenMatches, WIDE_SCREEN_QUERY } from '@/lib/motion';

export interface Film {
  /** The clip: it ends on the room the still picture is a detail of. */
  video: string;
  /** The clip's first frame, shown while it loads. */
  poster: string;
  /** Where in the last frame the detail sits (a CSS transform-origin): the camera leans in there. */
  focus: string;
}

/** film: the clip plays; detail: it ended and the picture comes in over it; still: only the picture. */
type Phase = 'film' | 'detail' | 'still';

/** Whether the film plays on this visit: wide screen, motion allowed, data not saved, not seen yet. */
const filmPlays = (film?: Film) =>
  !!film && motionAllowed('authFilm') && screenMatches(WIDE_SCREEN_QUERY) && !savesData() && !wasSeen(film.video);

interface AuthFilmProps {
  image: string;
  film?: Film;
  /** Called once the picture is in place: right away without the film, at its end with it. */
  onSettled: () => void;
}

/**
 * The picture half of the sign-in and registration pages. With a film, the clip plays once (muted,
 * no controls: it is decoration) and the still picture, a detail of the room the clip ends in, comes
 * in over its last frame. Without it - or on a small screen, with less motion, a failed or blocked
 * clip - the picture alone, as before.
 */
const AuthFilm = ({ image, film, onSettled }: AuthFilmProps) => {
  const [phase, setPhase] = useState<Phase>(() => (filmPlays(film) ? 'film' : 'still'));
  const video = useRef<HTMLVideoElement>(null);

  useEffect(() => {
    if (phase === 'film') {
      // Autoplay can be refused (battery saver): the picture then
      video.current?.play().catch(() => setPhase('still'));
      return;
    }
    if (film && phase === 'detail') markSeen(film.video);
    onSettled();
  }, [phase, film, onSettled]);

  return (
    <>
      {phase !== 'still' && film && (
        <video
          ref={video}
          src={film.video}
          poster={film.poster}
          muted
          playsInline
          preload="auto"
          aria-hidden
          tabIndex={-1}
          onEnded={() => setPhase('detail')}
          onError={() => setPhase('still')}
          style={{ transformOrigin: film.focus }}
          className={cn('absolute inset-0 h-full w-full object-cover', phase === 'detail' && 'animate-film-lean')}
        />
      )}
      <img
        src={image}
        alt=""
        style={film && { transformOrigin: film.focus }}
        className={cn(
          'absolute inset-0 h-full w-full object-cover',
          phase === 'film' && 'opacity-0',
          phase === 'detail' && 'animate-film-detail',
          phase === 'still' && 'animate-zoom-out',
        )}
      />
    </>
  );
};

export default AuthFilm;
