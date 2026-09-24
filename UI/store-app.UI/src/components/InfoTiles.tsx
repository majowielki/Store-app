import { usePerks } from '@/content/perks';
import { cn } from '@/lib/utils';
import Reveal from './Reveal';

/** What every order gets, in a row of four under a hairline. */
const InfoTiles = ({ className }: { className?: string }) => {
  const perks = usePerks();
  return (
    <section className={cn('align-element py-16', className)}>
      <ul className="grid border-y sm:grid-cols-2 lg:grid-cols-4 lg:divide-x">
        {perks.map(({ key, icon: Icon, title, description }, index) => (
          <Reveal as="li" key={key} delay={index * 80} className="flex items-start gap-4 border-b py-8 last:border-b-0 sm:[&:nth-last-child(-n+2)]:border-b-0 lg:border-b-0 lg:px-8 lg:first:pl-0">
            <span className="grid h-12 w-12 shrink-0 place-items-center rounded-full bg-secondary">
              <Icon className="h-5 w-5" />
            </span>
            <div>
              <h3 className="font-medium">{title}</h3>
              <p className="mt-1 text-sm text-muted-foreground">{description}</p>
            </div>
          </Reveal>
        ))}
      </ul>
    </section>
  );
};

export default InfoTiles;
