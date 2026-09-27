import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetProductsMetaQuery, useGetProductsQuery } from '@/api/catalog';
import { useGetPricingRulesQuery } from '@/api/orders';
import hero1 from '@/assets/hero1.webp';
import hero2 from '@/assets/hero2.webp';
import { useAppSelector } from '@/hooks';
import CountUp from './CountUp';
import { Button } from './ui/button';

/**
 * The spinning round badge on the hero picture: the welcome discount for visitors (leading to
 * the registration), the sale for customers who already have an account.
 */
const RotatingBadge = () => {
  const user = useAppSelector((s) => s.session.user);
  const { data: rules } = useGetPricingRulesQuery();
  const welcome = user ? undefined : rules;
  const phrase = welcome ? `${welcome.firstOrderDiscountPercent}% off your first order` : 'Shop the sale now';
  return (
    <Link
      to={welcome ? '/register' : '/products?sale=on'}
      aria-label={phrase}
      className="group absolute -right-3 -top-8 grid h-32 w-32 place-items-center rounded-full bg-brand text-brand-foreground shadow-xl transition-transform duration-500 ease-smooth hover:scale-105 md:-right-8 md:h-36 md:w-36"
    >
      <svg viewBox="0 0 120 120" className="absolute inset-0 h-full w-full animate-spin-slow" aria-hidden>
        <defs>
          <path id="hero-badge-circle" d="M60,60 m-45,0 a45,45 0 1,1 90,0 a45,45 0 1,1 -90,0" />
        </defs>
        <text className="fill-current text-[9.5px] font-medium uppercase tracking-[0.18em]">
          <textPath href="#hero-badge-circle" textLength="280">
            {phrase} • {phrase} •
          </textPath>
        </text>
      </svg>
      <ArrowUpRight className="h-7 w-7 transition-transform duration-500 ease-smooth group-hover:rotate-45" />
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

const Hero = () => (
  <section className="align-element grid items-center gap-16 overflow-x-clip pb-20 pt-10 lg:grid-cols-12 lg:gap-8 lg:pb-28 lg:pt-14">
    <div className="lg:col-span-7">
      <p className="eyebrow flex animate-fade-up items-center gap-3">
        <span className="h-px w-8 bg-foreground/40" />
        Furniture &amp; objects for every room
      </p>
      <h1 className="display mt-6 text-[clamp(3.25rem,8.2vw,7.75rem)] leading-[0.9]">
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

    <div className="relative mx-auto w-full max-w-md lg:col-span-5 lg:max-w-none">
      <div className="relative aspect-4/5 overflow-hidden rounded-4xl bg-muted">
        <img
          src={hero1}
          alt="A light linen sofa with a woven pouf and brass side tables"
          className="h-full w-full animate-zoom-out object-cover"
        />
      </div>
      <div className="absolute -bottom-8 -left-6 w-36 animate-fade-up [animation-delay:700ms] md:-left-12 md:w-44">
        <div className="animate-float overflow-hidden rounded-2xl border-[6px] border-background shadow-2xl">
          <img src={hero2} alt="" className="aspect-4/5 w-full object-cover" />
        </div>
      </div>
      <RotatingBadge />
    </div>
  </section>
);

export default Hero;
