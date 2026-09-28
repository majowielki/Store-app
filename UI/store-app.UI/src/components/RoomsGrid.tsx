import { Link } from 'react-router-dom';
import ResponsiveImage from '@/components/ResponsiveImage';
import { ArrowUpRight } from 'lucide-react';
import { useGetProductsQuery } from '@/api/catalog';
import { cn } from '@/lib/utils';
import { categories, categoryHref, priceTag, type Category } from '@/utils';
import Reveal from './Reveal';

// Where each room sits in the four-column mosaic (two columns on phones)
const layout: Record<string, string> = {
  furniture: 'col-span-2 row-span-2',
  bedroom: '',
  kitchen: '',
  bathroom: '',
  decorations: '',
  kids: '',
  garden: '',
};

/** A room shows the picture of its first product, the room around it included. */
const RoomTile = ({ category, className, delay }: { category: Category; className?: string; delay: number }) => {
  const { data } = useGetProductsQuery({ group: category.group, pageSize: 1 });
  const image = data?.items[0]?.image;
  return (
    <Reveal delay={delay} className={cn('min-h-0', className)}>
      <Link
        to={categoryHref(category)}
        className="group relative block h-full overflow-hidden rounded-3xl bg-muted focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-4"
      >
        {image && (
          <ResponsiveImage size="card" placeholder
            src={image}
            alt=""
            loading="lazy"
            className="absolute inset-0 h-full w-full object-cover transition-transform [transition-duration:1200ms] ease-smooth group-hover:scale-[1.07]"
          />
        )}
        <div className="absolute inset-0 bg-linear-to-t from-black/65 via-black/10 to-transparent" />
        <div className="absolute inset-x-0 bottom-0 flex items-end justify-between gap-3 p-4 text-white md:p-6">
          <div>
            {data && <p className="text-[0.65rem] uppercase tracking-[0.2em] text-white/70">{data.totalCount} pieces</p>}
            <p className="display text-2xl md:text-4xl">{category.label}</p>
          </div>
          <span className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-white/15 backdrop-blur-md transition-colors duration-500 group-hover:bg-white group-hover:text-black md:h-12 md:w-12">
            <ArrowUpRight className="h-5 w-5 transition-transform duration-500 ease-smooth group-hover:rotate-45" />
          </span>
        </div>
      </Link>
    </Reveal>
  );
};

/** The sale's own tile: terracotta, with the deepest discount the shop has right now. */
const SaleTile = ({ delay }: { delay: number }) => {
  const { data } = useGetProductsQuery({ sale: 'true' });
  const deepest = Math.max(0, ...(data?.items ?? []).map((p) => priceTag(p).percent));
  return (
    <Reveal delay={delay} className="col-span-2">
      <Link
        to="/products?sale=on"
        className="group relative flex h-full flex-col justify-between overflow-hidden rounded-3xl bg-brand p-6 text-brand-foreground md:p-8"
      >
        <span className="absolute -right-16 -top-16 h-64 w-64 rounded-full border border-current opacity-20 transition-transform duration-1000 ease-smooth group-hover:scale-125" />
        <span className="absolute -right-4 -top-4 h-40 w-40 rounded-full border border-current opacity-20 transition-transform duration-1000 ease-smooth group-hover:scale-150" />
        <p className="text-[0.65rem] uppercase tracking-[0.2em] opacity-75">{data ? `${data.totalCount} pieces reduced` : 'Reduced'}</p>
        <div className="flex items-end justify-between gap-4">
          <p className="display text-4xl md:text-6xl">
            Sale{deepest > 0 && <em className="block text-2xl opacity-80 md:ml-3 md:inline md:text-4xl">up to -{deepest}%</em>}
          </p>
          <span className="grid h-12 w-12 shrink-0 place-items-center rounded-full bg-brand-foreground text-brand transition-transform duration-500 ease-smooth group-hover:rotate-45">
            <ArrowUpRight className="h-5 w-5" />
          </span>
        </div>
      </Link>
    </Reveal>
  );
};

/** "Shop by room": the groups of the catalogue as a mosaic of pictures. */
const RoomsGrid = () => {
  const rooms = categories.filter((c) => c.group !== 'sale');
  return (
    <section className="align-element py-20 md:py-28" aria-labelledby="rooms">
      <Reveal as="header" className="flex flex-wrap items-end justify-between gap-6">
        <div>
          <p className="eyebrow">Shop by room</p>
          <h2 id="rooms" className="display mt-3 text-4xl md:text-6xl">
            Start with the room, <em>not the catalogue.</em>
          </h2>
        </div>
        <Link to="/products" className="group inline-flex items-center gap-2 text-sm font-medium">
          <span className="link-underline">Everything we make</span>
          <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" />
        </Link>
      </Reveal>
      <div className="mt-12 grid auto-rows-44 grid-cols-2 gap-3 md:auto-rows-60 md:gap-4 lg:grid-cols-4">
        {rooms.map((room, index) => (
          <RoomTile key={room.slug} category={room} className={layout[room.group]} delay={(index % 4) * 80} />
        ))}
        <SaleTile delay={160} />
      </div>
    </section>
  );
};

export default RoomsGrid;
