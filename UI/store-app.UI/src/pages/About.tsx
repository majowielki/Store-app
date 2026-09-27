import { Link } from 'react-router-dom';
import { ArrowUpRight, Leaf, ShieldCheck, Sparkles, Truck } from 'lucide-react';
import joinery from '@/assets/about-joinery.webp';
import linen from '@/assets/about-linen.webp';
import CountUp from '@/components/CountUp';
import Reveal from '@/components/Reveal';
import { Button } from '@/components/ui/button';
import { useParallax } from '@/hooks/use-parallax';

const values = [
  { icon: Leaf, title: 'Sustainable materials', text: 'We choose environmentally friendly resources, and wood from forests that are replanted.' },
  { icon: ShieldCheck, title: 'Trusted quality', text: 'Craftsmanship and attention to detail, backed by a two-year warranty.' },
  { icon: Truck, title: 'Fast delivery', text: 'Most orders arrive within 48 hours, and returns are free for thirty days.' },
  { icon: Sparkles, title: 'Timeless design', text: 'Forms that never go out of style, in colours that sit well together.' },
];

const stats: { label: string; count?: number; text: string }[] = [
  { label: 'happy customers', count: 10, text: 'k+' },
  { label: 'average rating', text: '4.9/5' },
  { label: 'delivery time', count: 48, text: 'h' },
];

const About = () => {
  const bandRef = useParallax<HTMLImageElement>(60);
  return (
    <div>
      <header className="max-w-4xl">
        <p className="eyebrow animate-fade-up">About us</p>
        <h1 className="display mt-6 text-[clamp(2.75rem,6.5vw,6rem)] leading-[0.95]">
          <span className="line-mask">
            <span>We design comfort</span>
          </span>
          <span className="line-mask">
            <span style={{ animationDelay: '90ms' }}>
              that looks as good <em className="text-brand">as it feels.</em>
            </span>
          </span>
        </h1>
        <p className="mt-8 max-w-xl animate-fade-up text-lg leading-relaxed text-muted-foreground [animation-delay:300ms]">
          From concept to delivery — we obsess over the details. Our products blend function with aesthetics so everyday
          life simply feels better.
        </p>
      </header>

      <Reveal className="mt-16 aspect-16/10 overflow-hidden rounded-4xl bg-muted md:aspect-21/9">
        <img ref={bandRef} src={joinery} alt="The corner of an oak dining table, where the top meets its angled leg" className="h-full w-full scale-[1.12] object-cover" />
      </Reveal>

      <dl className="mt-16 grid gap-8 border-y py-12 sm:grid-cols-3">
        {stats.map((stat, index) => (
          <Reveal key={stat.label} delay={index * 100} className="flex flex-col-reverse items-center text-center">
            <dt className="eyebrow mt-3">{stat.label}</dt>
            <dd className="display text-6xl md:text-7xl">
              {stat.count !== undefined && <CountUp value={stat.count} />}
              {stat.text}
            </dd>
          </Reveal>
        ))}
      </dl>

      <section className="mt-24 grid items-center gap-16 lg:grid-cols-2">
        <div>
          <Reveal>
            <p className="eyebrow">What we care about</p>
            <h2 className="display mt-3 text-4xl leading-[1.05] md:text-5xl">
              Four things we <em>never</em> compromise on.
            </h2>
          </Reveal>
          <ul className="mt-10 grid gap-4 sm:grid-cols-2">
            {values.map(({ icon: Icon, title, text }, index) => (
              <Reveal as="li" key={title} delay={index * 80} className="rounded-2xl border bg-card p-6 transition-colors hover:border-foreground/30">
                <span className="grid h-11 w-11 place-items-center rounded-full bg-secondary">
                  <Icon className="h-5 w-5" />
                </span>
                <p className="mt-5 font-medium">{title}</p>
                <p className="mt-1.5 text-sm leading-relaxed text-muted-foreground">{text}</p>
              </Reveal>
            ))}
          </ul>
        </div>
        <Reveal className="relative">
          <div className="aspect-4/5 overflow-hidden rounded-4xl bg-muted">
            <img src={linen} alt="Stonewashed linen bedding up close" loading="lazy" className="h-full w-full object-cover" />
          </div>
          <div className="absolute -bottom-6 left-6 flex flex-wrap gap-2">
            {['Ethically sourced', '2-year warranty', 'Free returns'].map((tag) => (
              <span key={tag} className="rounded-full bg-background/90 px-4 py-2 text-xs font-medium shadow-lg backdrop-blur-sm">
                {tag}
              </span>
            ))}
          </div>
        </Reveal>
      </section>

      <Reveal className="mt-28 flex flex-col items-center text-center">
        <h2 className="display max-w-2xl text-4xl md:text-6xl">
          Ready to find <em>your</em> piece?
        </h2>
        <Button asChild size="lg" className="group mt-8">
          <Link to="/products">
            Explore the shop
            <ArrowUpRight className="transition-transform duration-500 ease-smooth group-hover:rotate-45" />
          </Link>
        </Button>
      </Reveal>
    </div>
  );
};
export default About;
