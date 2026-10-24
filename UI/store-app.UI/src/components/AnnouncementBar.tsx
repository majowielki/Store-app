import { useEffect, useState } from 'react';
import { Asterisk, Pause, Play } from 'lucide-react';
import { usePerks } from '@/content/perks';
import { REDUCED_MOTION_QUERY, screenMatches } from '@/lib/motion';

/** How long one perk stays on a narrow screen before the next comes in. */
const PERK_SHOWN_MS = 5000;

/**
 * The slim bar above the header with what every order gets. A wide screen has room for all the
 * perks at once, standing still; a narrow one shows them one after another. Content that changes
 * by itself must be possible to stop, so the narrow bar has a pause button; visitors who asked
 * for less motion get the first perk alone, still.
 */
const AnnouncementBar = () => {
  const perks = usePerks();
  const [paused, setPaused] = useState(false);
  const [shown, setShown] = useState(0);

  useEffect(() => {
    if (paused || screenMatches(REDUCED_MOTION_QUERY)) return;
    const timer = window.setInterval(() => setShown((index) => (index + 1) % perks.length), PERK_SHOWN_MS);
    return () => window.clearInterval(timer);
  }, [paused, perks.length]);

  return (
    <aside aria-label="What every order gets" className="relative bg-primary text-primary-foreground">
      <ul className="grid place-items-center px-10 py-2 text-xs tracking-wide lg:flex lg:justify-center lg:gap-10">
        {perks.map((perk, index) => (
          <li
            key={perk.key}
            data-shown={index === shown}
            className="col-start-1 row-start-1 flex items-center gap-3 transition-[opacity,transform] duration-700 ease-smooth max-lg:data-[shown=false]:translate-y-2 max-lg:data-[shown=false]:opacity-0"
          >
            <Asterisk className="h-3.5 w-3.5 text-brand" aria-hidden />
            {perk.announcement}
          </li>
        ))}
      </ul>
      <button
        type="button"
        onClick={() => setPaused((value) => !value)}
        aria-pressed={paused}
        aria-label="Pause the offers"
        title={paused ? 'Play' : 'Pause'}
        className="absolute inset-y-0 right-0 grid w-8 place-items-center bg-primary transition-colors hover:text-brand focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-current motion-reduce:hidden lg:hidden"
      >
        {paused ? <Play className="h-3.5 w-3.5" aria-hidden /> : <Pause className="h-3.5 w-3.5" aria-hidden />}
      </button>
    </aside>
  );
};

export default AnnouncementBar;
