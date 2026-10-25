import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetProductsMetaQuery, useGetProductsQuery } from '@/api/catalog';
import { useGetPricingRulesQuery } from '@/api/orders';
import hero1 from '@/assets/hero1.webp';
import heroLight from '@/assets/cinematic/hero-light.mp4';
import heroEvening from '@/assets/cinematic/hero-evening.mp4';
import heroEveningPicture from '@/assets/cinematic/hero-evening.webp';
import { useAppSelector } from '@/hooks';
import HeroLight, { type HeroScene } from './cinematic/HeroLight';
import CountUp from './CountUp';
import { Button } from './ui/button';

/**
 * The round badge on the hero picture: the welcome discount for visitors (leading to
 * the registration), the sale for customers who already have an account.
 */
const WelcomeBadge = () => {
  const user = useAppSelector((s) => s.session.user);
  const { data: rules } = useGetPricingRulesQuery();
  const welcome = user ? undefined : rules;
  const phrase = welcome ? `${welcome.firstOrderDiscountPercent}% off your first order` : 'Shop the sale now';
  return (
    <Link
      to={welcome ? '/register' : '/products?sale=on'}
      className="group absolute bottom-4 left-4 grid h-24 w-24 place-items-center rounded-full bg-brand text-brand-foreground shadow-xl transition-transform duration-500 ease-smooth hover:scale-105 md:h-32 md:w-32 lg:bottom-12 lg:left-12"
    >
      <svg viewBox="0 0 120 120" className="absolute inset-0 h-full w-full" aria-hidden>
        <defs>
          <path id="hero-badge-circle" d="M60,60 m-45,0 a45,45 0 1,1 90,0 a45,45 0 1,1 -90,0" />
        </defs>
        <text className="fill-current text-[9.5px] font-medium uppercase tracking-[0.18em]">
          <textPath href="#hero-badge-circle" textLength="280">
            {phrase} • {phrase} •
          </textPath>
        </text>
      </svg>
      {/* The circle repeats the phrase for the eye; a screen reader hears it once */}
      <span className="sr-only">{phrase}</span>
      <ArrowUpRight className="h-7 w-7 transition-transform duration-500 ease-smooth group-hover:rotate-45" aria-hidden />
    </Link>
  );
};

/** The shop in numbers, straight from the catalogue. */
const Stats = () => {
  const { data: page } = useGetProductsQuery({ pageSize: 1 });
  const { data: meta } = useGetProductsMetaQuery();
  const stats = [
    { label: 'Pieces', value: page?.totalCount },
    { label: 'Makers', value: meta?.companies.filter((c) => c !== 'all').length },
    { label: 'Rooms', value: meta?.groups.filter((g) => g !== 'all').length },
  ];
  return (
    <dl className="mt-14 grid max-w-md animate-fade-up grid-cols-3 gap-6 border-t pt-6 [animation-delay:650ms]">
      {stats.map((stat) => (
        <div key={stat.label}>
          <dt className="eyebrow">{stat.label}</dt>
          <dd className="display mt-1 text-4xl">{stat.value ? <CountUp value={stat.value} /> : '—'}</dd>
        </div>
      ))}
    </dl>
  );
};

// The living room in the morning (light theme) and the same room at sunset (dark theme)
const morning: HeroScene = { image: hero1, video: heroLight, width: 1200, height: 1800 };
const evening: HeroScene = { image: heroEveningPicture, video: heroEvening, width: 1200, height: 1200 };

/**
 * The first screen: the promise on the left, in the page's column, and the living room on the
 * right half, out to the edge of the screen, with the morning light moving across its wall.
 */
const Hero = () => (
  <section className="grid gap-10 pb-20 pt-4 sm:pt-6 lg:min-h-[min(54rem,calc(100svh-6.5rem))] lg:grid-cols-2 lg:gap-0 lg:pb-0 lg:pt-0">
    <div className="flex flex-col justify-center px-4 sm:px-6 lg:py-16 lg:pl-[max(2rem,calc((100vw-80rem)/2+2rem))] lg:pr-16">
      <p className="eyebrow flex animate-fade-up items-center gap-3">
        <span className="h-px w-8 bg-foreground/40" />
        Furniture &amp; objects for every room
      </p>
      <h1 className="display mt-6 text-[clamp(3.25rem,6.6vw,7rem)] leading-[0.9]">
        <span className="line-mask">
          <span>Make every</span>
        </span>
        <span className="line-mask">
          <span style={{ animationDelay: '90ms' }}>room feel</span>
        </span>
        <span className="line-mask">
          <span style={{ animationDelay: '180ms' }}>
            like <em className="text-brand">home.</em>
          </span>
        </span>
      </h1>
      <p className="mt-8 max-w-md animate-fade-up text-lg leading-relaxed text-muted-foreground [animation-delay:420ms]">
        Considered furniture and objects for living, sleeping and gathering — delivered to your door, with thirty days to
        change your mind.
      </p>
      <div className="mt-10 flex animate-fade-up flex-wrap items-center gap-3 [animation-delay:520ms]">
        <Button asChild size="lg" className="group pr-2">
          <Link to="/products">
            Shop the collection
            <span className="ml-2 grid h-8 w-8 place-items-center rounded-full bg-primary-foreground text-primary transition-transform duration-500 ease-smooth group-hover:rotate-45">
              <ArrowUpRight />
            </span>
          </Link>
        </Button>
        <Button asChild size="lg" variant="outline">
          <Link to="/products?sale=on">Browse the sale</Link>
        </Button>
      </div>
      <Stats />
    </div>

    {/* On a phone the room comes first, so the first screen shows what the shop sells */}
    <div className="relative order-first mx-4 sm:mx-6 lg:order-none lg:mx-0">
      <HeroLight
        morning={morning}
        evening={evening}
        alt="A light linen sofa with a woven pouf and brass side tables"
        className="aspect-5/4 rounded-3xl sm:aspect-4/3 lg:aspect-auto lg:h-full lg:rounded-none lg:rounded-bl-[3rem]"
      />
      <WelcomeBadge />
    </div>
  </section>
);

export default Hero;
