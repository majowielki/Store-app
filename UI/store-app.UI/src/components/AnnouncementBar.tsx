import { useState } from 'react';
import { Asterisk, Pause, Play } from 'lucide-react';
import { usePerks } from '@/content/perks';
import Marquee from './Marquee';

/**
 * The slim ticker above the header with what every order gets. Text that keeps moving must be
 * possible to stop, so the bar has a pause button; it is not needed (nor shown) for visitors
 * who asked for less motion, as the ticker stands still for them.
 */
const AnnouncementBar = () => {
  const perks = usePerks();
  const [paused, setPaused] = useState(false);
  // Twice over, so one copy is wider than any screen
  const items = [...perks, ...perks];
  return (
    <aside aria-label="What every order gets" className="relative bg-primary text-primary-foreground">
      <Marquee duration="60s" paused={paused} className="py-2 text-xs tracking-wide">
        {items.map((perk, index) => (
          <span key={`${perk.key}-${index}`} className="flex items-center gap-6 px-6">
            <Asterisk className="h-3.5 w-3.5 text-brand" aria-hidden />
            {perk.announcement}
          </span>
        ))}
      </Marquee>
      <button
        type="button"
        onClick={() => setPaused((value) => !value)}
        aria-pressed={paused}
        aria-label="Pause the offers"
        title={paused ? 'Play' : 'Pause'}
        className="absolute inset-y-0 right-0 grid w-8 place-items-center bg-primary transition-colors hover:text-brand focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-current motion-reduce:hidden"
      >
        {paused ? <Play className="h-3.5 w-3.5" aria-hidden /> : <Pause className="h-3.5 w-3.5" aria-hidden />}
      </button>
    </aside>
  );
};

export default AnnouncementBar;
