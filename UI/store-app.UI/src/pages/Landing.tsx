import { Fragment } from 'react';
import { Link } from 'react-router-dom';
import { Asterisk } from 'lucide-react';
import { useGetProductsMetaQuery } from '@/api/catalog';
import { Hero, FeaturedProducts, InfoTiles } from '@/components';
import Editorial from '@/components/Editorial';
import Marquee from '@/components/Marquee';
import PromoBand from '@/components/PromoBand';
import RoomsGrid from '@/components/RoomsGrid';
import { cn } from '@/lib/utils';

/** Every category of the catalogue, large, drifting past between the hero and the rooms. */
const CategoryTicker = () => {
  const { data: meta } = useGetProductsMetaQuery();
  const entries = (meta?.groupCategoryMap ?? []).flatMap((group) =>
    group.categories.map((c) => ({ key: `${group.key}-${c.key}`, name: c.name, to: `/products?group=${encodeURIComponent(group.key)}&category=${encodeURIComponent(c.key)}` })),
  );
  if (entries.length === 0) return <div className="h-[6.5rem] border-y md:h-[8.5rem]" />;

  return (
    <div className="border-y py-6 md:py-8">
      <Marquee duration="90s">
        {entries.map((entry, index) => (
          <Fragment key={entry.key}>
            <Link
              to={entry.to}
              className={cn(
                'display whitespace-nowrap px-6 text-5xl leading-none transition-colors hover:text-brand md:px-10 md:text-7xl',
                index % 2 === 1 && 'italic text-muted-foreground',
              )}
            >
              {entry.name}
            </Link>
            <Asterisk className="h-8 w-8 shrink-0 text-brand md:h-10 md:w-10" aria-hidden />
          </Fragment>
        ))}
      </Marquee>
    </div>
  );
};

const Landing = () => (
  <>
    <Hero />
    <CategoryTicker />
    <RoomsGrid />
    <FeaturedProducts />
    <Editorial />
    <PromoBand />
    <InfoTiles />
  </>
);
export default Landing;
