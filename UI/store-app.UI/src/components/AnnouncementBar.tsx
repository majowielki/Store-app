import { Asterisk } from 'lucide-react';
import { usePerks } from '@/content/perks';
import Marquee from './Marquee';

/** The slim ticker above the header with what every order gets. */
const AnnouncementBar = () => {
  const perks = usePerks();
  // Twice over, so one copy is wider than any screen
  const items = [...perks, ...perks];
  return (
    <div className="bg-primary text-primary-foreground">
      <Marquee duration="60s" className="py-2 text-xs tracking-wide">
        {items.map((perk, index) => (
          <span key={`${perk.key}-${index}`} className="flex items-center gap-6 px-6">
            <Asterisk className="h-3.5 w-3.5 text-brand" aria-hidden />
            {perk.announcement}
          </span>
        ))}
      </Marquee>
    </div>
  );
};

export default AnnouncementBar;
